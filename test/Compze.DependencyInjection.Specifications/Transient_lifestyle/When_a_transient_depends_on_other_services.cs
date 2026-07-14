using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;
using Compze.Must.Assertions;

namespace Compze.DependencyInjection.Specifications.Transient_lifestyle;

public class When_a_transient_depends_on_other_services
{
   [DependencyInjectionContainerMatrix]
   public void transient_can_depend_on_a_singleton()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         Singleton.For<ISingletonDependency>().CreatedBy(() => new SingletonDependency()),
         TrackedTransient.For<IServiceWithDependency>().CreatedBy((ISingletonDependency dep) => new ServiceWithDependency(dep))
      );

      using var container = builder.Build();
      var first = container.Resolve<IServiceWithDependency>();
      var second = container.Resolve<IServiceWithDependency>();

      first.Must().NotBe(second);
      ((ServiceWithDependency)first).Dependency.Must().Be(((ServiceWithDependency)second).Dependency);
   }

   [DependencyInjectionContainerMatrix]
   public void transient_can_depend_on_another_transient()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         TrackedTransient.For<ITransientDependency>().CreatedBy(() => new TransientDependency()),
         TrackedTransient.For<IServiceWithTransientDependency>().CreatedBy((ITransientDependency dep) => new ServiceWithTransientDependency(dep))
      );

      using var container = builder.Build();
      var first = (ServiceWithTransientDependency)container.Resolve<IServiceWithTransientDependency>();
      var second = (ServiceWithTransientDependency)container.Resolve<IServiceWithTransientDependency>();

      first.Must().NotBe(second);
      first.Dependency.Must().NotBe(second.Dependency);
   }

   interface ISingletonDependency;
   class SingletonDependency : ISingletonDependency;

   interface IServiceWithDependency;
   class ServiceWithDependency(ISingletonDependency dependency) : IServiceWithDependency
   {
      public ISingletonDependency Dependency { get; } = dependency;
   }

   interface ITransientDependency;
   class TransientDependency : ITransientDependency;

   interface IServiceWithTransientDependency;
   class ServiceWithTransientDependency(ITransientDependency dependency) : IServiceWithTransientDependency
   {
      public ITransientDependency Dependency { get; } = dependency;
   }
}
