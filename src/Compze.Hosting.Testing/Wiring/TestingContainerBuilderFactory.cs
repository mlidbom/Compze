using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.DependencyInjection.Autofac;
using Compze.DependencyInjection.Autofac.Extensions.Hosting;
using Compze.DependencyInjection.DryIoc;
using Compze.DependencyInjection.DryIoc.Extensions.Hosting;
using Compze.DependencyInjection.LightInject;
using Compze.DependencyInjection.LightInject.Extensions.Hosting;
using Compze.DependencyInjection.Microsoft;
using Compze.DependencyInjection.Microsoft.Extensions.Hosting;
using Compze.DependencyInjection.Wiring;
using Compze.Underscore;

namespace Compze.Hosting.Testing.Wiring;

public static class TestingContainerBuilderFactory
{
   ///<summary>
   /// Creates a container builder for this DI container technology, set up for testing: its registrar is a
   /// <see cref="TestingComponentRegistrar"/> (so production wiring resolves connection strings through the test
   /// database pool), and the container technology's host integration is pre-registered (so transport servers can
   /// use the container as their service provider). No Compze components are registered yet.
   ///</summary>
   public static IContainerBuilder CreateTestingContainerBuilder(this DIContainer @this) =>
      @this switch
      {
         DIContainer.Microsoft   => new MicrosoftContainerBuilder(new TestingComponentRegistrar())._mutate(it => MicrosoftChildContainerHostIntegration.RegisterWith(it.Registrar)),
         DIContainer.Autofac     => new AutofacContainerBuilder(new TestingComponentRegistrar())._mutate(it => AutofacChildContainerHostIntegration.RegisterWith(it.Registrar)),
         DIContainer.DryIoc      => new DryIocContainerBuilder(new TestingComponentRegistrar())._mutate(it => DryIocChildContainerHostIntegration.RegisterWith(it.Registrar)),
         DIContainer.LightInject => new LightInjectContainerBuilder(new TestingComponentRegistrar())._mutate(it => LightInjectChildContainerHostIntegration.RegisterWith(it.Registrar)),
         _                       => throw new ArgumentOutOfRangeException()
      };
}
