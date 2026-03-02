using System;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Utilities.DependencyInjection;

public class InvalidLifeStyleCombinationException : Exception
{
   internal InvalidLifeStyleCombinationException(ComponentRegistration parent, ComponentRegistration dependency, Type dependencyType)
      : base($"Invalid lifestyle combination: {nameof(Lifestyle.Singleton)}: {parent.InstantiationSpec.FactoryMethodReturnType.FullName} depends on {dependency.Lifestyle.ToString()}: {dependencyType.FullName}") {}
}
