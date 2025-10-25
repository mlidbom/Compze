using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Teventive.EventStore.Public;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ReflectionCE;
using Compze.Utilities.SystemCE.ReflectionCE.EmitCE;
using Compze.Utilities.Testing.XUnit.BDD;
using Compze.Utilities.Threading;
using Compze.Utilities.Threading.ResourceAccess;
using FluentAssertions;


namespace Compze.Tests.ScratchPad.ReflectionEmit;

public interface IUserWrapperEvent<out TWrappedUserEvent> : IWrapperEvent<TWrappedUserEvent>
   where TWrappedUserEvent : IUserEvent;

public interface IUserEvent : IEvent;

class UserEvent : IUserEvent;

public class Example
{
   [XF] public void BuildWrapperEventType()
   {
      var genericWrapperEventType = CreateGenericWrapperEventType(typeof(IUserWrapperEvent<>));

      //instantiate a concrete version.
      var wrapperEventIUserEvent = genericWrapperEventType.MakeGenericType(typeof(IUserEvent));

      var constructor = (Func<IUserEvent, IUserWrapperEvent<IUserEvent>>)Constructor.Compile.ForReturnType(wrapperEventIUserEvent).WithArgumentTypes(typeof(IUserEvent));

      var userEvent = new UserEvent();
      var instance = constructor(userEvent);

      instance.Event.Should().Be(userEvent);
   }


   static IReadOnlyDictionary<Type, Type> _createdWrapperTypes = new Dictionary<Type, Type>();
   static readonly MonitorCE Monitor = MonitorCE.WithDefaultTimeout();
   static Type CreateGenericWrapperEventType(Type wrapperEventType)
   {
      if(_createdWrapperTypes.TryGetValue(wrapperEventType, out var cachedWrapperImplementation))
      {
         return cachedWrapperImplementation;
      }

      return Monitor.Update(() =>
      {
         if(_createdWrapperTypes.TryGetValue(wrapperEventType, out cachedWrapperImplementation))
         {
            return cachedWrapperImplementation;
         }

         if(!wrapperEventType.IsInterface) throw new ArgumentException("Must be an interface", $"{nameof(wrapperEventType)}");
         if(wrapperEventType.GetInterfaces().All(iface => iface != typeof(IWrapperEvent<>).MakeGenericType(wrapperEventType.GetGenericArguments()[0])))
            throw new ArgumentException($"Must implement {typeof(IWrapperEvent<>).FullName}", $"{nameof(wrapperEventType)}");

         var wrappedEventType = wrapperEventType.GetGenericArguments()[0];

         var requiredEventInterface = wrappedEventType.GetGenericParameterConstraints().Single(constraint => constraint.IsInterface && typeof(IEvent).IsAssignableFrom(constraint));

         var genericWrapperEventType = AssemblyBuilderCE.Module.Update(module =>
         {
            var wrapperEventBuilder = module.DefineType(
               name: $"{wrapperEventType}_ilgen_impl",
               attr: TypeAttributes.Public,
               parent: null,
               interfaces: [wrapperEventType]);

            var wrappedEventTypeParameter = wrapperEventBuilder.DefineGenericParameters("TWrappedEvent")[0];

            wrappedEventTypeParameter.SetInterfaceConstraints(requiredEventInterface);

            var (wrappedEventField, _) = wrapperEventBuilder.ImplementProperty(nameof(IWrapperEvent<IAggregateEvent>.Event), wrappedEventTypeParameter);

            wrapperEventBuilder.ImplementConstructor(wrappedEventField);

            return wrapperEventBuilder.CreateType().NotNull();
         });

         OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _createdWrapperTypes, wrapperEventType, genericWrapperEventType);

         return genericWrapperEventType;
      });
   }
}