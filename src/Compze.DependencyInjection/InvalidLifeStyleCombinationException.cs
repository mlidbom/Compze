using Compze.DependencyInjection.Abstractions;

namespace Compze.DependencyInjection;

public class InvalidLifeStyleCombinationException : Exception
{
   internal InvalidLifeStyleCombinationException(ComponentRegistration consumer, ComponentRegistration dependency, Type dependencyType)
      : base($"""
              Invalid lifestyle combination: {consumer.Lifestyle}: {consumer.InstantiationSpec.FactoryMethodReturnType.FullName} depends on {dependency.Lifestyle}: {dependencyType.FullName}
              You have two escape hatches for {nameof(Lifestyle.TrackedTransient)} components if this is intentional call one or both of these when registering the component: 
                * .{nameof(TransientRegistrationWithoutInstantiationSpec<>.AllowSingletonDependent)}()
                * .{nameof(TransientRegistrationWithoutInstantiationSpec<>.AllowScopedDependent)}()
              """) {}

   InvalidLifeStyleCombinationException(string message) : base(message) {}

   ///<summary>The refusal of a service that <paramref name="consumer"/> resolves from the container's root, as its<br/>
   /// <see cref="ComponentRegistration.DependencyTypesResolvedFromTheRoot"/> declare, when the root can't resolve it:<br/>
   /// <paramref name="fromTheRootToTheScopedService"/> runs from that service, through the tracked transients the root would resolve<br/>
   /// for it, to the scoped service it takes, or is that service alone when it is scoped itself.</summary>
   internal static InvalidLifeStyleCombinationException ResolvingAScopedServiceFromTheRoot(ComponentRegistration consumer, IReadOnlyList<(Type ServiceType, ComponentRegistration Registration)> fromTheRootToTheScopedService)
   {
      var resolvedFromTheRoot = fromTheRootToTheScopedService[0];
      var whyTheRootCantResolveIt = fromTheRootToTheScopedService.Count == 1
                                       ? $"it is registered as {Lifestyle.Scoped}"
                                       : $"its {resolvedFromTheRoot.Registration.Lifestyle} registration takes {WhatItTakes([..fromTheRootToTheScopedService.Skip(1)])}";
      return new InvalidLifeStyleCombinationException($"Invalid lifestyle combination: {consumer.Lifestyle}: {consumer.InstantiationSpec.FactoryMethodReturnType.FullName} resolves {resolvedFromTheRoot.ServiceType.FullName} from the container's root, which can't resolve a {Lifestyle.Scoped} service, and {whyTheRootCantResolveIt}.");

      static string WhatItTakes(IReadOnlyList<(Type ServiceType, ComponentRegistration Registration)> taken) =>
         string.Concat(taken.Select((it, index) => index < taken.Count - 1
                                                      ? $"{it.ServiceType.FullName}, a {it.Registration.Lifestyle} that takes "
                                                      : $"{it.ServiceType.FullName}, which is {it.Registration.Lifestyle}"));
   }
}
