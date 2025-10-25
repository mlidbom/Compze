using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Teventive.EventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ReflectionCE;
using Compze.Utilities.SystemCE.ReflectionCE.EmitCE;
using Compze.Utilities.Testing.XUnit.BDD;
using Compze.Utilities.Threading;
using Compze.Utilities.Threading.ResourceAccess;
using FluentAssertions;


namespace Compze.Tests.ScratchPad.ReflectionEmit;

public interface IUserWrapperTevent<out TWrappedUserEvent> : IWrapperTevent<TWrappedUserEvent>
   where TWrappedUserEvent : IUserTevent;

public interface IUserTevent : ITevent;

class UserTevent : IUserTevent;

public class Example
{
   [XF] public void BuildWrapperEventType()
   {
      var genericWrapperEventType = CreateGenericWrapperEventType(typeof(IUserWrapperTevent<>));

      //instantiate a concrete version.
      var wrapperEventIUserEvent = genericWrapperEventType.MakeGenericType(typeof(IUserTevent));

      var constructor = (Func<IUserTevent, IUserWrapperTevent<IUserTevent>>)Constructor.Compile.ForReturnType(wrapperEventIUserEvent).WithArgumentTypes(typeof(IUserTevent));

      var userEvent = new UserTevent();
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
         if(wrapperEventType.GetInterfaces().All(iface => iface != typeof(IWrapperTevent<>).MakeGenericType(wrapperEventType.GetGenericArguments()[0])))
            throw new ArgumentException($"Must implement {typeof(IWrapperTevent<>).FullName}", $"{nameof(wrapperEventType)}");

         var wrappedEventType = wrapperEventType.GetGenericArguments()[0];

         var requiredEventInterface = wrappedEventType.GetGenericParameterConstraints().Single(constraint => constraint.IsInterface && typeof(ITevent).IsAssignableFrom(constraint));

         var genericWrapperEventType = AssemblyBuilderCE.Module.Update(module =>
         {
            var wrapperEventBuilder = module.DefineType(
               name: $"{wrapperEventType}_ilgen_impl",
               attr: TypeAttributes.Public,
               parent: null,
               interfaces: [wrapperEventType]);

            var wrappedEventTypeParameter = wrapperEventBuilder.DefineGenericParameters("TWrappedEvent")[0];

            wrappedEventTypeParameter.SetInterfaceConstraints(requiredEventInterface);

            var (wrappedEventField, _) = wrapperEventBuilder.ImplementProperty(nameof(IWrapperTevent<IAggregateTevent>.Event), wrappedEventTypeParameter);

            wrapperEventBuilder.ImplementConstructor(wrappedEventField);

            return wrapperEventBuilder.CreateType().NotNull();
         });

         OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _createdWrapperTypes, wrapperEventType, genericWrapperEventType);

         return genericWrapperEventType;
      });
   }
}