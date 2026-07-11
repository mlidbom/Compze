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
}
