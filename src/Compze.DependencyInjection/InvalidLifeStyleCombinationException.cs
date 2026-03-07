using Compze.DependencyInjection.Abstractions;

namespace Compze.DependencyInjection;

public class InvalidLifeStyleCombinationException : Exception
{
   internal InvalidLifeStyleCombinationException(ComponentRegistration consumer, ComponentRegistration dependency, Type dependencyType)
      : base($"Invalid lifestyle combination: {consumer.Lifestyle}: {consumer.InstantiationSpec.FactoryMethodReturnType.FullName} depends on {dependency.Lifestyle}: {dependencyType.FullName}") {}
}
