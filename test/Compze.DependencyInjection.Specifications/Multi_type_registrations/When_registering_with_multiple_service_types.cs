using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;

namespace Compze.DependencyInjection.Specifications.Multi_type_registrations;

interface IServiceA;
interface IServiceB;
class MultiTypeService : IServiceA, IServiceB;

public class When_registering_with_multiple_service_types
{
   [DependencyInjectionContainerMatrix]
   public void Singleton_resolved_by_different_types_returns_same_instance()
   {
      using var container = DependencyInjectionContainerFactory.CreateContainer();
      container.Register(
         Singleton.For<IServiceA, IServiceB>()
                  .CreatedBy(() => new MultiTypeService()));

      var serviceLocator = container.ServiceLocator;

      var resolvedAsA = serviceLocator.Resolve<IServiceA>();
      var resolvedAsB = serviceLocator.Resolve<IServiceB>();

      resolvedAsA.Must().Be(resolvedAsB);
   }

   [DependencyInjectionContainerMatrix]
   public void Scoped_resolved_by_different_types_within_same_scope_returns_same_instance()
   {
      using var container = DependencyInjectionContainerFactory.CreateContainer();
      container.Register(
         Scoped.For<IServiceA, IServiceB>()
               .CreatedBy(() => new MultiTypeService()));

      var serviceLocator = container.ServiceLocator;

      using(serviceLocator.BeginScope())
      {
         var resolvedAsA = serviceLocator.Resolve<IServiceA>();
         var resolvedAsB = serviceLocator.Resolve<IServiceB>();

         resolvedAsA.Must().Be(resolvedAsB);
      }
   }

   [DependencyInjectionContainerMatrix]
   public void Scoped_resolved_by_different_types_in_different_scopes_returns_different_instances()
   {
      using var container = DependencyInjectionContainerFactory.CreateContainer();
      container.Register(
         Scoped.For<IServiceA, IServiceB>()
               .CreatedBy(() => new MultiTypeService()));

      var serviceLocator = container.ServiceLocator;

      IServiceA fromScope1;
      using(serviceLocator.BeginScope())
      {
         fromScope1 = serviceLocator.Resolve<IServiceA>();
      }

      using(serviceLocator.BeginScope())
      {
         var fromScope2 = serviceLocator.Resolve<IServiceA>();
         fromScope2.Must().NotBe(fromScope1);
      }
   }

   [DependencyInjectionContainerMatrix]
   public void Singleton_with_dependency_resolved_by_different_types_returns_same_instance()
   {
      using var container = DependencyInjectionContainerFactory.CreateContainer();
      container.Register(
         Singleton.For<IServiceA, IServiceB>()
                  .CreatedBy((IDependency dep) => new MultiTypeServiceWithDep(dep)),
         Singleton.For<IDependency>()
                  .CreatedBy(() => new Dependency()));

      var serviceLocator = container.ServiceLocator;

      var resolvedAsA = serviceLocator.Resolve<IServiceA>();
      var resolvedAsB = serviceLocator.Resolve<IServiceB>();

      resolvedAsA.Must().Be(resolvedAsB);
   }

   [DependencyInjectionContainerMatrix]
   public void Singleton_resolved_by_different_types_across_scopes_returns_same_instance()
   {
      using var container = DependencyInjectionContainerFactory.CreateContainer();
      container.Register(
         Singleton.For<IServiceA, IServiceB>()
                  .CreatedBy(() => new MultiTypeService()));

      var serviceLocator = container.ServiceLocator;

      IServiceA resolvedAsA;
      using(serviceLocator.BeginScope())
      {
         resolvedAsA = serviceLocator.Resolve<IServiceA>();
      }

      using(serviceLocator.BeginScope())
      {
         var resolvedAsB = serviceLocator.Resolve<IServiceB>();
         resolvedAsB.Must().Be(resolvedAsA);
      }
   }
}

interface IDependency;
class Dependency : IDependency;
class MultiTypeServiceWithDep(IDependency dep) : IServiceA, IServiceB
{
   // ReSharper disable once UnusedMember.Global
   public IDependency Dep { get; } = dep;
}
