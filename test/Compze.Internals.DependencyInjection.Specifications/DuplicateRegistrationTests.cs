using Compze.Internals.DependencyInjection.Specifications.Infrastructure;
using Compze.Utilities.DependencyInjection;
using Compze.Must;

namespace Compze.Internals.DependencyInjection.Specifications;

public class DuplicateRegistrationTests
{
   interface ITestService;
   class TestService : ITestService;
   interface ITestService2;
   class MultiService : ITestService, ITestService2;

   [DependencyInjectionContainerMatrix]
   public void Registering_same_singleton_service_twice_throws_InvalidOperationException()
   {
      using var container = DependencyInjectionContainerFactory.CreateContainer();

      container.Register(Singleton.For<ITestService>().CreatedBy(() => new TestService()));

      container.Invoking(it => it.Register(Singleton.For<ITestService>().CreatedBy(() => new TestService())))
               .Must()
               .Throw<InvalidOperationException>()
               .Which.Message.Must()
               .Contain("ITestService")
               .Contain("already registered");
   }

   [DependencyInjectionContainerMatrix]
   public void Registering_same_scoped_service_twice_throws_InvalidOperationException()
   {
      using var container = DependencyInjectionContainerFactory.CreateContainer();

      container.Register(Scoped.For<ITestService>().CreatedBy(() => new TestService()));

      container.Invoking(it => it.Register(Scoped.For<ITestService>().CreatedBy(() => new TestService())))
               .Must()
               .Throw<InvalidOperationException>().Which.Message.Must()
               .Contain("ITestService")
               .Contain("already registered");
   }

   [DependencyInjectionContainerMatrix]
   public void Registering_service_with_multiple_service_types_then_reregistering_one_throws_InvalidOperationException()
   {
      using var container = DependencyInjectionContainerFactory.CreateContainer();

      container.Register(
         Singleton.For<ITestService, ITestService2>()
                  .CreatedBy(() => new MultiService()));

      container.Invoking(it => it.Register(Singleton.For<ITestService>().CreatedBy(() => new TestService())))
               .Must()
               .Throw<InvalidOperationException>()
               .Which.Message.Must()
               .Contain("ITestService")
               .Contain("already registered");
   }
}
