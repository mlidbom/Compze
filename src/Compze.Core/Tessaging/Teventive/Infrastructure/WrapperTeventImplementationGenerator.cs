using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Compze.Contracts;
using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ReflectionCE;
using Compze.Utilities.SystemCE.ReflectionCE.EmitCE;
using Compze.Utilities.SystemCE.ThreadingCE;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

// ReSharper disable UnusedTypeParameter : todo: We'll keep the unused stuff for reference for now. If it is not used during the refactoring of wrapped tevent dispatching, ditch it.
// ReSharper disable UnusedMember.Global

namespace Compze.Core.Tessaging.Teventive.Infrastructure;

public static class WrapperTeventImplementationGenerator
{
   static IReadOnlyDictionary<Type, Func<ITevent, IPublisherIdentifyingTevent<ITevent>>> _wrapperConstructors = new Dictionary<Type, Func<ITevent, IPublisherIdentifyingTevent<ITevent>>>();
   static IReadOnlyDictionary<Type, Type> _createdWrapperTypes = new Dictionary<Type, Type>();

   static string DescribeParameterList(IEnumerable<Type> parameterTypes) => parameterTypes.Select(parameterType => parameterType.FullNameNotNull()).Join(", ");

   static readonly IMonitorCE MonitorCE = IMonitorCE.WithDefaultTimeout();

   public static class WrapperConstructorCache<TWrapperTevent, TWrappedTevent>
      where TWrapperTevent : IPublisherIdentifyingTevent<TWrappedTevent>
      where TWrappedTevent : ITevent
   {
      static readonly Func<ITevent, IPublisherIdentifyingTevent<ITevent>> UntypedConstructor = MonitorCE.Locked(() => CreateConstructorFor(typeof(TWrappedTevent)));

      public static readonly Func<TWrappedTevent, IPublisherIdentifyingTevent<TWrappedTevent>> Constructor = tevent => (IPublisherIdentifyingTevent<TWrappedTevent>)UntypedConstructor(tevent);
   }

   public static TWrapperTevent WrapTevent<TWrapperTevent, TWrappedTevent>(TWrappedTevent theTevent)
      where TWrapperTevent : IPublisherIdentifyingTevent<TWrappedTevent>
      where TWrappedTevent : ITevent =>
      (TWrapperTevent)WrapperConstructorCache<TWrapperTevent, TWrappedTevent>.Constructor(theTevent);

   public static IPublisherIdentifyingTevent<TWrappedTevent> WrapTevent<TWrappedTevent>(TWrappedTevent theTevent) where TWrappedTevent : ITevent =>
      WrapperConstructorCache<IPublisherIdentifyingTevent<TWrappedTevent>, TWrappedTevent>.Constructor(theTevent);

   // Todo: The fact that we can wrap like this, without the types of the wrapping tevents, does that not also mean that we could, eventually, receive tevents on the bus without having the type information for all the wrapping tevents to deserialize to?
   // Note the eventually though! This is not a priority, but certainly something to keep in mind. If we can dig out just the inner tevent and wrap it like this, a listening endpoint need only know
   // the types for the inner tevent that it listens to, not the types in which it is wrapped. Just a heads-up so we don't remove this strange code when we implement taggregates more cleanly. This still has great potential...
   public static Func<ITevent, IPublisherIdentifyingTevent<ITevent>> ConstructorFor(Type wrappedTeventType) =>
      MonitorCE.DoubleCheckedLocking(
         tryRead: () => _wrapperConstructors.GetValueOrDefault(wrappedTeventType),
         updateOnFailedRead: () => OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _wrapperConstructors, wrappedTeventType, CreateConstructorFor(wrappedTeventType))
      );

   static Func<ITevent, IPublisherIdentifyingTevent<ITevent>> CreateConstructorFor(Type wrappedTeventType)
   {
      var openWrapperTeventType = typeof(IPublisherIdentifyingTevent<>);
      var closedWrapperTeventType = openWrapperTeventType.MakeGenericType(wrappedTeventType);

      var openWrapperImplementationType = CreateGenericWrapperTeventImplementationClass(openWrapperTeventType);
      var closedWrapperImplementationType = openWrapperImplementationType.MakeGenericType(wrappedTeventType);

      var constructorArgumentTypes = new[] { wrappedTeventType };

      var constructor = closedWrapperImplementationType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, binder: null, types: constructorArgumentTypes, modifiers: null);
      if(constructor == null)
      {
         throw new Exception($"Expected to find a constructor with the signature: [private|protected|public] {closedWrapperTeventType.GetFullNameCompilable()}({DescribeParameterList(constructorArgumentTypes)})");
      }

      var parameter = Expression.Parameter(typeof(ITevent), "tevent");
      var castParameter = Expression.Convert(parameter, wrappedTeventType);
      var constructorCall = Expression.New(constructor, castParameter);
      var lambda = Expression.Lambda<Func<ITevent, IPublisherIdentifyingTevent<ITevent>>>(constructorCall, parameter);

      return lambda.Compile();
   }

   static Type CreateGenericWrapperTeventImplementationClass(Type wrapperTeventType)
   {
      if(_createdWrapperTypes.TryGetValue(wrapperTeventType, out var cachedWrapperImplementation))
      {
         return cachedWrapperImplementation;
      }

      if(!wrapperTeventType.IsInterface) throw new ArgumentException("Must be an interface", $"{nameof(wrapperTeventType)}");

      if(wrapperTeventType != typeof(IPublisherIdentifyingTevent<>)
      && wrapperTeventType.GetInterfaces().All(iface => iface != typeof(IPublisherIdentifyingTevent<>).MakeGenericType(wrapperTeventType.GetGenericArguments()[0])))
         throw new ArgumentException($"Must implement {typeof(IPublisherIdentifyingTevent<>).FullName}", $"{nameof(wrapperTeventType)}");

      var wrappedTeventType = wrapperTeventType.GetGenericArguments()[0];

      var requiredTeventInterface = wrappedTeventType.GetGenericParameterConstraints().Single(constraint => constraint.IsInterface && typeof(ITevent).IsAssignableFrom(constraint));

      var genericWrapperTeventType = AssemblyBuilderCE.Module.Locked(module =>
      {
         var wrapperTeventBuilder = module.DefineType(
            name: $"{wrapperTeventType}_compze_generated_implementation",
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
   }
}
