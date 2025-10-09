using System;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using FluentAssertions;
using Xunit;

namespace Compze.Tests.Integration.XUnit.DependencyInjection;

public class DuplicateRegistrationTests : DuplicateByPluggableComponentTest
{
    interface ITestService;
    class TestService : ITestService;
    interface ITestService2;
    class TestService2 : ITestService2;
    class MultiService : ITestService, ITestService2;

    static IDependencyInjectionContainer CreateContainer(Compze.Wiring.DIContainer diContainer)
    {
        var runMode = new RunMode(isTesting: true);
        return diContainer switch
        {
            Compze.Wiring.DIContainer.Microsoft => new Compze.Utilities.DependencyInjection.Microsoft.MicrosoftDependencyInjectionContainer(runMode),
            Compze.Wiring.DIContainer.SimpleInjector => new Compze.Utilities.DependencyInjection.SimpleInjector.SimpleInjectorDependencyInjectionContainer(runMode),
            _ => throw new NotSupportedException($"DI Container {diContainer} not supported")
        };
    }

    [PluggableComponentsTheory]
    public void Registering_same_singleton_service_twice_throws_InvalidOperationException(PluggableComponentTestContext context)
    {
        // This test runs for each combination but focuses on DI container behavior
        // It demonstrates using the context.DIContainer value
        
        var container = CreateContainer(context.DIContainer);

        container.Register(Singleton.For<ITestService>().CreatedBy(() => new TestService()));

        var attemptingDuplicateRegistration = () => container.Register(Singleton.For<ITestService>().CreatedBy(() => new TestService()));

        attemptingDuplicateRegistration.Should()
           .Throw<InvalidOperationException>()
           .WithMessage("*ITestService*")
           .WithMessage("*already*registered*");
    }

    [PluggableComponentsTheory]
    public void Registering_same_scoped_service_twice_throws_InvalidOperationException(PluggableComponentTestContext context)
    {
        var container = CreateContainer(context.DIContainer);

        container.Register(Scoped.For<ITestService>().CreatedBy(() => new TestService()));

        var attemptingDuplicateRegistration = () => container.Register(Scoped.For<ITestService>().CreatedBy(() => new TestService()));

        attemptingDuplicateRegistration.Should()
           .Throw<InvalidOperationException>()
           .WithMessage("*ITestService*")
           .WithMessage("*already*registered*");
    }

    [PluggableComponentsTheory]
    public void Registering_service_with_multiple_service_types_then_reregistering_one_throws_InvalidOperationException(PluggableComponentTestContext context)
    {
        var container = CreateContainer(context.DIContainer);

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
    public void Can_register_different_service_types_successfully(PluggableComponentTestContext context)
    {
        var container = CreateContainer(context.DIContainer);

        container.Register(Singleton.For<ITestService>().CreatedBy(() => new TestService()));

        var registeringDifferentServiceType = () => container.Register(Singleton.For<ITestService2>().CreatedBy(() => new TestService2()));

        registeringDifferentServiceType.Should().NotThrow();
    }
}
