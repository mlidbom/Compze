using Compze.DependencyInjection.Runtime;
using Compze.DependencyInjection.Runtime.Resolution;
using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.DependencyInjection.Wiring.Registration;
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
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         Singleton.For<IServiceA, IServiceB>()
                  .CreatedBy(() => new MultiTypeService()));

      using var container = builder.Build();

      var resolvedAsA = container.Resolve<IServiceA>();
      var resolvedAsB = container.Resolve<IServiceB>();

      resolvedAsA.Must().Be(resolvedAsB);
   }

   [DependencyInjectionContainerMatrix]
   public void Scoped_resolved_by_different_types_within_same_scope_returns_same_instance()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         Scoped.For<IServiceA, IServiceB>()
               .CreatedBy(() => new MultiTypeService()));

      using var container = builder.Build();

      using var scope = container.BeginScope();
      var resolvedAsA = scope.Resolve<IServiceA>();
      var resolvedAsB = scope.Resolve<IServiceB>();

      resolvedAsA.Must().Be(resolvedAsB);
   }

   [DependencyInjectionContainerMatrix]
   public void Scoped_resolved_by_different_types_in_different_scopes_returns_different_instances()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         Scoped.For<IServiceA, IServiceB>()
               .CreatedBy(() => new MultiTypeService()));

      using var container = builder.Build();

      IServiceA fromScope1;
      {
         using var scope1 = container.BeginScope();
         fromScope1 = scope1.Resolve<IServiceA>();
      }

      {
         using var scope2 = container.BeginScope();
         var fromScope2 = scope2.Resolve<IServiceA>();
         fromScope2.Must().NotBe(fromScope1);
      }
   }

   [DependencyInjectionContainerMatrix]
   public void Singleton_with_dependency_resolved_by_different_types_returns_same_instance()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         Singleton.For<IServiceA, IServiceB>()
                  .CreatedBy((IDependency dep) => new MultiTypeServiceWithDep(dep)),
         Singleton.For<IDependency>()
                  .CreatedBy(() => new Dependency()));

      using var container = builder.Build();

      var resolvedAsA = container.Resolve<IServiceA>();
      var resolvedAsB = container.Resolve<IServiceB>();

      resolvedAsA.Must().Be(resolvedAsB);
   }

   [DependencyInjectionContainerMatrix]
   public void Singleton_resolved_by_different_types_across_scopes_returns_same_instance()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         Singleton.For<IServiceA, IServiceB>()
                  .CreatedBy(() => new MultiTypeService()));

      using var container = builder.Build();

      IServiceA resolvedAsA;
      {
         using var scope1 = container.BeginScope();
         resolvedAsA = scope1.Resolve<IServiceA>();
      }

      {
         using var scope2 = container.BeginScope();
         var resolvedAsB = scope2.Resolve<IServiceB>();
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
