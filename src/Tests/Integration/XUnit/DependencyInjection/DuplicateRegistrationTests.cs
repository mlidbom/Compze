using System;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.DependencyInjection;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.Infrastructure.XUnit.PluggableComponents;
using Compze.Utilities.DependencyInjection;
using FluentAssertions;
using Xunit;

namespace Compze.Tests.Integration.XUnit.DependencyInjection;

public class DuplicateRegistrationTests : UniversalTestBase
{
   interface ITestService;
   class TestService : ITestService;
   interface ITestService2;
   class TestService2 : ITestService2;
   class MultiService : ITestService, ITestService2;

   [PluggableComponentsTheory]
   public void Registering_same_singleton_service_twice_throws_InvalidOperationException()
   {
      var container = TestEnv.DIContainer.Create();

      container.Register(Singleton.For<ITestService>().CreatedBy(() => new TestService()));

      var attemptingDuplicateRegistration = () => container.Register(Singleton.For<ITestService>().CreatedBy(() => new TestService()));

      attemptingDuplicateRegistration.Should()
                                     .Throw<InvalidOperationException>()
                                     .WithMessage("*ITestService*")
                                     .WithMessage("*already*registered*");
   }

   [PluggableComponentsTheory]
   public void Registering_same_scoped_service_twice_throws_InvalidOperationException()
   {
      var container = TestEnv.DIContainer.Create();

      container.Register(Scoped.For<ITestService>().CreatedBy(() => new TestService()));

      var attemptingDuplicateRegistration = () => container.Register(Scoped.For<ITestService>().CreatedBy(() => new TestService()));

      attemptingDuplicateRegistration.Should()
                                     .Throw<InvalidOperationException>()
                                     .WithMessage("*ITestService*")
                                     .WithMessage("*already*registered*");
   }

   [PluggableComponentsTheory]
   public void Registering_service_with_multiple_service_types_then_reregistering_one_throws_InvalidOperationException()
   {
      var container = TestEnv.DIContainer.Create();

      container.Register(
         Singleton.For<ITestService, ITestService2>()
                  .CreatedBy(() => new MultiService()));

      var attemptingToReregisterOneServiceType = () => container.Register(Singleton.For<ITestService>().CreatedBy(() => new TestService()));

      attemptingToReregisterOneServiceType.Should()
                                          .Throw<InvalidOperationException>()
                                          .WithMessage("*ITestService*")
                                          .WithMessage("*already*registered*");
   }

   [PluggableComponentsTheory]
   public void Can_register_different_service_types_successfully()
   {
      var container = TestEnv.DIContainer.Create();

      container.Register(Singleton.For<ITestService>().CreatedBy(() => new TestService()));

      var registeringDifferentServiceType = () => container.Register(Singleton.For<ITestService2>().CreatedBy(() => new TestService2()));

      registeringDifferentServiceType.Should().NotThrow();
   }
}
