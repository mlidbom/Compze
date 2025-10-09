using System;
using Compze.Tessaging.Hosting.Testing.DependencyInjection;
using Compze.TestInfrastructure;
using Compze.Utilities.DependencyInjection;
using FluentAssertions;
using NUnit.Framework;
using Compze.TestInfrastructure.NUnit;

namespace Compze.Tests.Integration.Internals.DependencyInjection;

public class LifestyleValidationTests(string pluggableComponentsCombination) : DuplicateByPluggableComponentTest(pluggableComponentsCombination)
{
   [Test]
   public void Should_throw_when_singleton_depends_on_scoped_service()
   {
      var container = TestingContainerFactory.Create(RunMode.Testing);

      var exception = Assert.Throws<InvalidLifeStyleCombinationException>(() =>
      {
         container.Register(
            Scoped.For<IScopedService>().CreatedBy(() => new ScopedService()),
            Singleton.For<ISingletonService>().CreatedBy((IScopedService scoped) => new SingletonServiceDependingOnScoped(scoped))
         );
      });

      exception!.Message.Should().Contain("Invalid lifestyle combination");
      exception.Message.Should().Contain("Singleton");
      exception.Message.Should().Contain("Scoped"); ;
   }

   [Test]
   public void Should_allow_singleton_depending_on_singleton()
   {
      var container = TestingContainerFactory.Create(RunMode.Testing);

      container.Register(
         Singleton.For<ISingletonDependency>().CreatedBy(() => new SingletonDependency()),
         Singleton.For<ISingletonService>().CreatedBy((ISingletonDependency dep) => new SingletonServiceWithSingletonDependency(dep))
      );

      var service = container.ServiceLocator.Resolve<ISingletonService>();
      service.Should().NotBeNull();
   }

   interface IScopedService {}
   class ScopedService : IScopedService {}

   interface ISingletonService {}
   class SingletonServiceDependingOnScoped(IScopedService _) : ISingletonService {}

   interface ISingletonDependency {}
   class SingletonDependency : ISingletonDependency {}

   class SingletonServiceWithSingletonDependency(ISingletonDependency dependency) : ISingletonService
   {
      public ISingletonDependency Dependency { get; } = dependency;
   }
}
