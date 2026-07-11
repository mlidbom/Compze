using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;
using static Compze.Must.MustActions;

namespace Compze.DependencyInjection.Specifications.Circular_dependencies;

public class When_breaking_a_circular_dependency_with_a_service_resolver
{
   [DependencyInjectionContainerMatrix]
   public void both_sides_of_a_singleton_cycle_resolve_and_are_wired_to_each_other()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         Singleton.For<IServiceA>().CreatedBy((IServiceResolver<IServiceB> serviceBResolver) => new ServiceA(serviceBResolver)),
         Singleton.For<IServiceB>().WithServiceResolver().CreatedBy((IServiceA serviceA) => new ServiceB(serviceA))
      );

      using var container = builder.Build();
      var serviceA = (ServiceA)container.Resolve<IServiceA>();
      var serviceB = container.Resolve<IServiceB>();

      serviceA.ResolveServiceB().Must().Be(serviceB);
      ((ServiceB)serviceB).InjectedServiceA.Must().Be(serviceA);
   }

   [DependencyInjectionContainerMatrix]
   public void the_resolver_returns_the_same_singleton_instance_every_call()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         Singleton.For<IServiceA>().CreatedBy((IServiceResolver<IServiceB> serviceBResolver) => new ServiceA(serviceBResolver)),
         Singleton.For<IServiceB>().WithServiceResolver().CreatedBy((IServiceA serviceA) => new ServiceB(serviceA))
      );

      using var container = builder.Build();
      var serviceA = (ServiceA)container.Resolve<IServiceA>();

      serviceA.ResolveServiceB().Must().Be(serviceA.ResolveServiceB());
   }

   [DependencyInjectionContainerMatrix]
   public void a_scoped_cycle_resolves_the_same_scoped_instances_within_the_scope()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         Scoped.For<IServiceA>().CreatedBy((IServiceResolver<IServiceB> serviceBResolver) => new ServiceA(serviceBResolver)),
         Scoped.For<IServiceB>().WithServiceResolver().CreatedBy((IServiceA serviceA) => new ServiceB(serviceA))
      );

      using var container = builder.Build();
      using var scope = container.BeginScope();
      var serviceA = (ServiceA)scope.Resolve<IServiceA>();
      var serviceB = scope.Resolve<IServiceB>();

      serviceA.ResolveServiceB().Must().Be(serviceB);
      ((ServiceB)serviceB).InjectedServiceA.Must().Be(serviceA);
   }

   [DependencyInjectionContainerMatrix]
   public void resolvers_in_different_scopes_return_the_scoped_instance_of_their_own_scope()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         Scoped.For<IServiceA>().CreatedBy((IServiceResolver<IServiceB> serviceBResolver) => new ServiceA(serviceBResolver)),
         Scoped.For<IServiceB>().WithServiceResolver().CreatedBy((IServiceA serviceA) => new ServiceB(serviceA))
      );

      using var container = builder.Build();
      using var firstScope = container.BeginScope();
      using var secondScope = container.BeginScope();

      var serviceBInFirstScope = ((ServiceA)firstScope.Resolve<IServiceA>()).ResolveServiceB();
      var serviceBInSecondScope = ((ServiceA)secondScope.Resolve<IServiceA>()).ResolveServiceB();

      serviceBInFirstScope.Must().NotBe(serviceBInSecondScope);
   }

   [DependencyInjectionContainerMatrix]
   public void a_singleton_may_not_take_a_service_resolver_of_a_scoped_service()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();

      var exception = Invoking(() =>
      {
         builder.Registrar.Register(
            Scoped.For<IServiceB>().WithServiceResolver().CreatedBy(() => new ServiceB(null!)),
            Singleton.For<IServiceA>().CreatedBy((IServiceResolver<IServiceB> serviceBResolver) => new ServiceA(serviceBResolver))
         );
         _ = builder.Build();
      }).Must().Throw<InvalidLifeStyleCombinationException>().Which;

      exception.Message.Must().Contain("Invalid lifestyle combination");
      exception.Message.Must().Contain("Singleton");
      exception.Message.Must().Contain("Scoped");
   }

   [DependencyInjectionContainerMatrix]
   public void a_cloned_container_resolves_the_cycle_against_the_clone()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         Singleton.For<IServiceA>().CreatedBy((IServiceResolver<IServiceB> serviceBResolver) => new ServiceA(serviceBResolver)),
         Singleton.For<IServiceB>().WithServiceResolver().CreatedBy((IServiceA serviceA) => new ServiceB(serviceA))
      );

      using var source = builder.Build();
      using var clone = source.CreateCloneContainerBuilder().Build();

      var sourceServiceA = source.Resolve<IServiceA>();
      var cloneServiceA = (ServiceA)clone.Resolve<IServiceA>();

      cloneServiceA.Must().NotBe(sourceServiceA);
      cloneServiceA.ResolveServiceB().Must().Be(clone.Resolve<IServiceB>());
   }

   [DependencyInjectionContainerMatrix]
   public void a_resolver_is_exposed_for_each_service_type_the_component_is_registered_under()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         Singleton.For<IFirstService, ISecondService>().WithServiceResolver().CreatedBy(() => new ComponentServingTwoServiceTypes())
      );

      using var container = builder.Build();
      var resolvedThroughFirstServiceResolver = (object)container.Resolve<IServiceResolver<IFirstService>>().Resolve();
      var resolvedThroughSecondServiceResolver = (object)container.Resolve<IServiceResolver<ISecondService>>().Resolve();

      resolvedThroughFirstServiceResolver.Must().Be(resolvedThroughSecondServiceResolver);
   }

   interface IServiceA;
   interface IServiceB;

   interface IFirstService;
   interface ISecondService;
   class ComponentServingTwoServiceTypes : IFirstService, ISecondService;

   class ServiceA : IServiceA
   {
      readonly IServiceResolver<IServiceB> _serviceBResolver;
      public ServiceA(IServiceResolver<IServiceB> serviceBResolver) => _serviceBResolver = serviceBResolver;
      public IServiceB ResolveServiceB() => _serviceBResolver.Resolve();
   }

   class ServiceB : IServiceB
   {
      public ServiceB(IServiceA injectedServiceA) => InjectedServiceA = injectedServiceA;
      public IServiceA InjectedServiceA { get; }
   }
}
