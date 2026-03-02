using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Compze.Contracts;
using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Utilities.SystemCE.ReflectionCE;
using Compze.Utilities.SystemCE.ReflectionCE.EmitCE;
using Compze.Threading;
using Compze.Threading.ResourceAccess;
using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;

namespace Compze.Tests.ScratchPad.ReflectionEmit;

[SuppressMessage("ReSharper", "MemberCanBeInternal")]
public interface IUserPublisherIdentifyingTevent<out TWrappedUserTevent> : IPublisherIdentifyingTevent<TWrappedUserTevent>
   where TWrappedUserTevent : IUserTevent;

#pragma warning disable CA1040 //avoid empty interfaces
public interface IUserTevent : ITevent;
#pragma warning restore CA1040 //avoid empty interfaces
class UserTevent : IUserTevent;

public class Example
{
   [XF] public void BuildWrapperTeventType()
   {
      var genericWrapperTeventType = CreateGenericWrapperTeventType(typeof(IUserPublisherIdentifyingTevent<>));

      //instantiate a concrete version.
      var wrapperTeventIUserTevent = genericWrapperTeventType.MakeGenericType(typeof(IUserTevent));

      var constructor = (Func<IUserTevent, IUserPublisherIdentifyingTevent<IUserTevent>>)Constructor.Compile.ForType(wrapperTeventIUserTevent).WithArguments<IUserTevent>();

      var userTevent = new UserTevent();
      var instance = constructor(userTevent);

      instance.Tevent.Must().Be(userTevent);
   }

   static IReadOnlyDictionary<Type, Type> _createdWrapperTypes = new Dictionary<Type, Type>();
   static readonly IMonitor MonitorCE = IMonitor.WithDefaultTimeout();

   static Type CreateGenericWrapperTeventType(Type wrapperTeventType)
   {
      if(_createdWrapperTypes.TryGetValue(wrapperTeventType, out var cachedWrapperImplementation))
      {
         return cachedWrapperImplementation;
      }

      return MonitorCE.Locked(() =>
      {
         if(_createdWrapperTypes.TryGetValue(wrapperTeventType, out cachedWrapperImplementation))
         {
            return cachedWrapperImplementation;
         }

         if(!wrapperTeventType.IsInterface) throw new ArgumentException("Must be an interface", $"{nameof(wrapperTeventType)}");
         if(wrapperTeventType.GetInterfaces().All(iface => iface != typeof(IPublisherIdentifyingTevent<>).MakeGenericType(wrapperTeventType.GetGenericArguments()[0])))
            throw new ArgumentException($"Must implement {typeof(IPublisherIdentifyingTevent<>).FullName}", $"{nameof(wrapperTeventType)}");

         var wrappedTeventType = wrapperTeventType.GetGenericArguments()[0];

         var requiredTeventInterface = wrappedTeventType.GetGenericParameterConstraints().Single(constraint => constraint.IsInterface && typeof(ITevent).IsAssignableFrom(constraint));

         var genericWrapperTeventType = AssemblyBuilderCE.Module.Locked(module =>
         {
            var wrapperTeventBuilder = module.DefineType(
               name: $"{wrapperTeventType}_ilgen_impl",
               attr: TypeAttributes.Public,
               parent: null,
               interfaces: [wrapperTeventType]);

            var wrappedTeventTypeParameter = wrapperTeventBuilder.DefineGenericParameters("TWrappedTevent")[0];

            wrappedTeventTypeParameter.SetInterfaceConstraints(requiredTeventInterface);

            var (wrappedTeventField, _) = wrapperTeventBuilder.ImplementProperty(nameof(IPublisherIdentifyingTevent<ITaggregateTevent>.Tevent), wrappedTeventTypeParameter);

            wrapperTeventBuilder.ImplementConstructor(wrappedTeventField);

            return wrapperTeventBuilder.CreateType()._assert().NotNull();
         });

         OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _createdWrapperTypes, wrapperTeventType, genericWrapperTeventType);

         return genericWrapperTeventType;
      });
   }
}
