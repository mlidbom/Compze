using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tests.Infrastructure;
using Compze.Utilities.DependencyInjection;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Utilities.DependencyInjection.Abstractions;
using FluentAssertions;
using static FluentAssertions.FluentActions;

namespace Compze.Tests.Integration.DependencyInjection;

public class LifestyleValidationTests : UniversalTestBase
{
   [PCT]
   public void Should_throw_when_singleton_depends_on_scoped_service()
   {
      IComponentRegistrar registrar = new TestingComponentRegistrar();
      var container = TestEnv.DIContainer.CreateWithServiceLocatorAndCurrentTestsPluggableComponents();

      var exception = Invoking(() =>
      {
         container.Register(
            Scoped.For<IScopedService>().CreatedBy(() => new ScopedService()),
            Singleton.For<ISingletonService>().CreatedBy((IScopedService scoped) => new SingletonServiceDependingOnScoped(scoped))
         );
      }).Should().Throw<InvalidLifeStyleCombinationException>().Which;

      exception.Message.Should().Contain("Invalid lifestyle combination");
      exception.Message.Should().Contain("Singleton");
      exception.Message.Should().Contain("Scoped");
   }

   [PCT]
   public void Should_allow_singleton_depending_on_singleton()
   {
      IComponentRegistrar registrar = new TestingComponentRegistrar();
      var container = TestEnv.DIContainer.CreateWithServiceLocatorAndCurrentTestsPluggableComponents();

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
#pragma warning disable CS9113 // Parameter is unread.
   class SingletonServiceDependingOnScoped(IScopedService _) : ISingletonService {}
#pragma warning restore CS9113 // Parameter is unread.

   interface ISingletonDependency {}
   class SingletonDependency : ISingletonDependency {}

   class SingletonServiceWithSingletonDependency(ISingletonDependency dependency) : ISingletonService
   {
      // ReSharper disable once UnusedMember.Local
      public ISingletonDependency Dependency { get; } = dependency;
   }
}
