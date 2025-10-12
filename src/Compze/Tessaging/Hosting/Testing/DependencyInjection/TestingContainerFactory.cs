using System;
using Compze.Utilities.DependencyInjection.Abstractions;
using JetBrains.Annotations;

namespace Compze.Tessaging.Hosting.Testing.DependencyInjection;

public static class TestingContainerFactory
{
   public static IDependencyInjectionContainer Create(IRunMode runMode) 
      => TestEnv.DIContainer.Create(runMode);
}