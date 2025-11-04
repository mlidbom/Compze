using System;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tests.Infrastructure;
using Compze.Utilities.DependencyInjection;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Utilities.Testing.Must;

namespace Compze.Tests.Integration.DependencyInjection;

public class DuplicateRegistrationTests : UniversalTestBase
{
   interface ITestService;
   class TestService : ITestService;
   interface ITestService2;
   class MultiService : ITestService, ITestService2;

   [PCT]
   public void Registering_same_singleton_service_twice_throws_InvalidOperationException()
   {
      var container = TestEnv.DIContainer.CreateWithServiceLocatorAndCurrentTestsPluggableComponents();

      container.Register(Singleton.For<ITestService>().CreatedBy(() => new TestService()));

      var attemptingDuplicateRegistration = () => container.Register(Singleton.For<ITestService>().CreatedBy(() => new TestService()));

      CaughtException<InvalidOperationException> tempQualifier = attemptingDuplicateRegistration.Must()
                                                                                                .Throw<InvalidOperationException>();
      tempQualifier.Which.Message.Must()
                   .Contain("ITestService")
                   .Contain("already registered");
   }

   [PCT]
   public void Registering_same_scoped_service_twice_throws_InvalidOperationException()
   {
      var container = TestEnv.DIContainer.CreateWithServiceLocatorAndCurrentTestsPluggableComponents();

      container.Register(Scoped.For<ITestService>().CreatedBy(() => new TestService()));

      var attemptingDuplicateRegistration = () => container.Register(Scoped.For<ITestService>().CreatedBy(() => new TestService()));

      CaughtException<InvalidOperationException> tempQualifier = attemptingDuplicateRegistration.Must()
                                                                                                .Throw<InvalidOperationException>();
      tempQualifier.Which.Message.Must()
                   .Contain("ITestService")
                   .Contain("already registered");
   }

   [PCT]
   public void Registering_service_with_multiple_service_types_then_reregistering_one_throws_InvalidOperationException()
   {
      var container = TestEnv.DIContainer.CreateWithServiceLocatorAndCurrentTestsPluggableComponents();

      container.Register(
         Singleton.For<ITestService, ITestService2>()
                  .CreatedBy(() => new MultiService()));

      var attemptingToReregisterOneServiceType = () => container.Register(Singleton.For<ITestService>().CreatedBy(() => new TestService()));

      CaughtException<InvalidOperationException> tempQualifier = attemptingToReregisterOneServiceType.Must()
                                                                                                     .Throw<InvalidOperationException>();
      tempQualifier.Which.Message.Must()
                   .Contain("ITestService")
                   .Contain("already registered");
   }
}
