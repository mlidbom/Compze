using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Compze.Persistence.EventStore;
using Compze.SystemCE;
using Compze.SystemCE.ReflectionCE;
using Compze.SystemCE.ReflectionCE.EmitCE;
using Compze.SystemCE.ThreadingCE;
using Compze.SystemCE.ThreadingCE.ResourceAccess;

namespace Compze.Messaging;

static class WrapperEventImplementationGenerator
{
   static IReadOnlyDictionary<Type, Func<IEvent, IWrapperEvent<IEvent>>> _wrapperConstructors = new Dictionary<Type, Func<IEvent, IWrapperEvent<IEvent>>>();
   static IReadOnlyDictionary<Type, Type> _createdWrapperTypes = new Dictionary<Type, Type>();

   static string DescribeParameterList(IEnumerable<Type> parameterTypes) => parameterTypes.Select(parameterType => parameterType.FullNameNotNull()).Join(", ");

   static readonly MonitorCE Monitor = MonitorCE.WithDefaultTimeout();

   static class WrapperConstructorCache<TWrapperEvent, TWrappedEvent>
      where TWrapperEvent : IWrapperEvent<TWrappedEvent>
      where TWrappedEvent : IEvent
   {
      static readonly Func<IEvent, IWrapperEvent<IEvent>> UntypedConstructor = Monitor.Update(() => CreateConstructorFor(typeof(TWrappedEvent)));

      internal static readonly Func<TWrappedEvent, IWrapperEvent<TWrappedEvent>> Constructor = @event => (IWrapperEvent<TWrappedEvent>)UntypedConstructor(@event);
   }

   public static TWrapperEvent WrapEvent<TWrapperEvent, TWrappedEvent>(TWrappedEvent theEvent)
      where TWrapperEvent : IWrapperEvent<TWrappedEvent>
      where TWrappedEvent : IEvent =>
      (TWrapperEvent)WrapperConstructorCache<TWrapperEvent, TWrappedEvent>.Constructor(theEvent);

   public static IWrapperEvent<TWrappedEvent> WrapEvent<TWrappedEvent>(TWrappedEvent theEvent) where TWrappedEvent : IEvent =>
      WrapperConstructorCache<IWrapperEvent<TWrappedEvent>, TWrappedEvent>.Constructor(theEvent);

   public static Func<IEvent, IWrapperEvent<IEvent>> ConstructorFor(Type wrappedEventType) =>
      Monitor.DoubleCheckedLocking(
         tryGetValue: () => _wrapperConstructors.GetValueOrDefault(wrappedEventType),
         setValue: () => ThreadSafe.AddToCopyAndReplace(ref _wrapperConstructors, wrappedEventType, CreateConstructorFor(wrappedEventType))
      );

   static Func<IEvent, IWrapperEvent<IEvent>> CreateConstructorFor(Type wrappedEventType)
   {
      var openWrapperEventType = typeof(IWrapperEvent<>);
      var closedWrapperEventType = openWrapperEventType.MakeGenericType(wrappedEventType);

      var openWrapperImplementationType = CreateGenericWrapperEventImplementationClass(openWrapperEventType);
      var closedWrapperImplementationType = openWrapperImplementationType.MakeGenericType(wrappedEventType);

      var constructorArgumentTypes = new[] { wrappedEventType };
      var creatorFunctionArgumentTypes = new[] { typeof(IEvent) };

      var constructor = closedWrapperImplementationType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, binder: null, types: constructorArgumentTypes, modifiers: null);
      if(constructor == null)
      {
         throw new Exception($"Expected to find a constructor with the signature: [private|protected|public] {closedWrapperEventType.GetFullNameCompilable()}({DescribeParameterList(constructorArgumentTypes)})");
      }

      var constructorCallMethod = new DynamicMethod(name: $"Generated_constructor_for_{closedWrapperEventType.Name}", returnType: closedWrapperEventType, parameterTypes: creatorFunctionArgumentTypes, owner: closedWrapperImplementationType);
      var ilGenerator = constructorCallMethod.GetILGenerator();
      ilGenerator.Emit(OpCodes.Ldarg_0);
      ilGenerator.Emit(OpCodes.Castclass, wrappedEventType);
      ilGenerator.Emit(OpCodes.Newobj, constructor);
      ilGenerator.Emit(OpCodes.Ret);

      return (Func<IEvent, IWrapperEvent<IEvent>>)constructorCallMethod.CreateDelegate(typeof(Func<IEvent, IWrapperEvent<IEvent>>));
   }

   static Type CreateGenericWrapperEventImplementationClass(Type wrapperEventType)
   {
      if(_createdWrapperTypes.TryGetValue(wrapperEventType, out var cachedWrapperImplementation))
      {
         return cachedWrapperImplementation;
      }

      if(!wrapperEventType.IsInterface) throw new ArgumentException("Must be an interface", $"{nameof(wrapperEventType)}");

      if(wrapperEventType != typeof(IWrapperEvent<>)
      && wrapperEventType.GetInterfaces().All(iface => iface != typeof(IWrapperEvent<>).MakeGenericType(wrapperEventType.GetGenericArguments()[0])))
         throw new ArgumentException($"Must implement {typeof(IWrapperEvent<>).FullName}", $"{nameof(wrapperEventType)}");

      var wrappedEventType = wrapperEventType.GetGenericArguments()[0];

      var requiredEventInterface = wrappedEventType.GetGenericParameterConstraints().Single(constraint => constraint.IsInterface && typeof(IEvent).IsAssignableFrom(constraint));

      var genericWrapperEventType = AssemblyBuilderCE.Module.Update(module =>
      {
         var wrapperEventBuilder = module.DefineType(
            name: $"{wrapperEventType}_compze_generated_implementation",
            attr: TypeAttributes.Public,
            parent: null,
            interfaces: [wrapperEventType]);

         var wrappedEventTypeParameter = wrapperEventBuilder.DefineGenericParameters("TWrappedEvent")[0];

         wrappedEventTypeParameter.SetInterfaceConstraints(requiredEventInterface);

         var (wrappedEventField, _) = wrapperEventBuilder.ImplementProperty(nameof(IWrapperEvent<IAggregateEvent>.Event), wrappedEventTypeParameter);

         wrapperEventBuilder.ImplementConstructor(wrappedEventField);

         return wrapperEventBuilder.CreateType().NotNull();
      });

      ThreadSafe.AddToCopyAndReplace(ref _createdWrapperTypes, wrapperEventType, genericWrapperEventType);

      return genericWrapperEventType;
   }
}
