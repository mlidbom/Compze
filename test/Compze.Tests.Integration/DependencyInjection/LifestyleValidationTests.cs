using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tests.Infrastructure;
using Compze.Utilities.DependencyInjection;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Must;
using static Compze.Must.MustActions;

namespace Compze.Tests.Integration.DependencyInjection;

public class LifestyleValidationTests : UniversalTestBase
{
   [PCTDIContainer]
   public void Should_throw_when_singleton_depends_on_scoped_service()
   {
      using var container = TestEnv.DIContainer.CreateWithServiceLocatorAndCurrentTestsPluggableComponents();

      var exception = Invoking(() =>
      {
         // ReSharper disable once AccessToDisposedClosure
         _ = container.Register(
            Scoped.For<IScopedService>().CreatedBy(() => new ScopedService()),
            Singleton.For<ISingletonService>().CreatedBy((IScopedService scoped) => new SingletonServiceDependingOnScoped(scoped))
         ).ServiceLocator;
      }).Must().Throw<InvalidLifeStyleCombinationException>().Which;

      exception.Message.Must().Contain("Invalid lifestyle combination");
      exception.Message.Must().Contain("Singleton");
      exception.Message.Must().Contain("Scoped");
   }


   interface IScopedService;
   class ScopedService : IScopedService;

   interface ISingletonService;
#pragma warning disable CS9113 // Parameter is unread.
   class SingletonServiceDependingOnScoped(IScopedService _) : ISingletonService;
#pragma warning restore CS9113 // Parameter is unread.

}
