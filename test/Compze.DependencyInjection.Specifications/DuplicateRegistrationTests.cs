using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;
using Compze.Must.Assertions;

namespace Compze.DependencyInjection.Specifications;

public class DuplicateRegistrationTests
{
   interface ITestService;
   class TestService : ITestService;
   interface ITestService2;
   class MultiService : ITestService, ITestService2;

   [DependencyInjectionContainerMatrix]
   public void Registering_same_singleton_service_twice_throws_InvalidOperationException()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();

      builder.Registrar.Register(Singleton.For<ITestService>().CreatedBy(() => new TestService()));

      builder.Invoking(it => it.Registrar.Register(Singleton.For<ITestService>().CreatedBy(() => new TestService())))
               .Must()
               .Throw<InvalidOperationException>()
               .Which.Message.Must()
               .Contain("ITestService")
               .Contain("already registered");
   }

   [DependencyInjectionContainerMatrix]
   public void Registering_same_scoped_service_twice_throws_InvalidOperationException()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();

      builder.Registrar.Register(Scoped.For<ITestService>().CreatedBy(() => new TestService()));

      builder.Invoking(it => it.Registrar.Register(Scoped.For<ITestService>().CreatedBy(() => new TestService())))
               .Must()
               .Throw<InvalidOperationException>().Which.Message.Must()
               .Contain("ITestService")
               .Contain("already registered");
   }

   [DependencyInjectionContainerMatrix]
   public void Registering_same_transient_service_twice_throws_InvalidOperationException()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();

      builder.Registrar.Register(TrackedTransient.For<ITestService>().CreatedBy(() => new TestService()));

      builder.Invoking(it => it.Registrar.Register(TrackedTransient.For<ITestService>().CreatedBy(() => new TestService())))
               .Must()
               .Throw<InvalidOperationException>()
               .Which.Message.Must()
               .Contain("ITestService")
               .Contain("already registered");
   }

   [DependencyInjectionContainerMatrix]
   public void Registering_same_service_twice_in_a_single_Register_call_throws_InvalidOperationException()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();

      builder.Invoking(it => it.Registrar.Register(
                  Singleton.For<ITestService>().CreatedBy(() => new TestService()),
                  Singleton.For<ITestService>().CreatedBy(() => new TestService())))
               .Must()
               .Throw<InvalidOperationException>()
               .Which.Message.Must()
               .Contain("ITestService")
               .Contain("already registered");
   }

   [DependencyInjectionContainerMatrix]
   public void Registering_service_with_multiple_service_types_then_reregistering_one_throws_InvalidOperationException()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();

      builder.Registrar.Register(
         Singleton.For<ITestService, ITestService2>()
                  .CreatedBy(() => new MultiService()));

      builder.Invoking(it => it.Registrar.Register(Singleton.For<ITestService>().CreatedBy(() => new TestService())))
               .Must()
               .Throw<InvalidOperationException>()
               .Which.Message.Must()
               .Contain("ITestService")
               .Contain("already registered");
   }

   [DependencyInjectionContainerMatrix]
   public void Registering_a_component_set_member_for_an_already_singularly_registered_service_type_throws_InvalidOperationException()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();

      builder.Registrar.Register(Singleton.For<ITestService>().CreatedBy(() => new TestService()));

      builder.Invoking(it => it.Registrar.Register(Singleton.ForSet<ITestService>().CreatedBy(() => new TestService())))
               .Must()
               .Throw<InvalidOperationException>()
               .Which.Message.Must()
               .Contain("ITestService")
               .Contain("already registered");
   }

   [DependencyInjectionContainerMatrix]
   public void Registering_a_singular_service_for_an_already_registered_component_set_type_throws_InvalidOperationException()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();

      builder.Registrar.Register(Singleton.ForSet<ITestService>().CreatedBy(() => new TestService()));

      builder.Invoking(it => it.Registrar.Register(Singleton.For<ITestService>().CreatedBy(() => new TestService())))
               .Must()
               .Throw<InvalidOperationException>()
               .Which.Message.Must()
               .Contain("ITestService")
               .Contain("already registered");
   }

   [DependencyInjectionContainerMatrix]
   public void Registering_two_component_set_members_for_the_same_component_set_type_does_not_throw()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();

      // An uncaught exception here fails the test — this is the assertion that registering two set members sharing a
      // component set's service type is allowed, unlike the singular case exercised by the tests above.
      builder.Registrar.Register(Singleton.ForSet<ITestService>().CreatedBy(() => new TestService()));
      builder.Registrar.Register(Singleton.ForSet<ITestService>().CreatedBy(() => new TestService()));
   }
}
