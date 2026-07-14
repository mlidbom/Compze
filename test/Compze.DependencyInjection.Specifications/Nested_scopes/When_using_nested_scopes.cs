using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;
using Compze.Must.Assertions;

namespace Compze.DependencyInjection.Specifications.Nested_scopes;

interface IScopedService;
class ScopedService : IScopedService;

interface IScopedCounter;

class ScopedCounter : IScopedCounter
{
   static int _nextValue;
   public int Value { get; } = Interlocked.Increment(ref _nextValue);
}

public class When_using_nested_scopes
{
   [DependencyInjectionContainerMatrix]
   public void inner_scope_can_resolve_scoped_services()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(Scoped.For<IScopedService>().CreatedBy(() => new ScopedService()));

      using var container = builder.Build();

      using var outerScope = container.BeginScope();
      using var innerScope = container.BeginScope();
      innerScope.Resolve<IScopedService>().Must().NotBeNull();
   }

   [DependencyInjectionContainerMatrix]
   public void inner_scope_resolves_different_scoped_instance_than_outer_scope()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(Scoped.For<IScopedCounter>().CreatedBy(() => new ScopedCounter()));

      using var container = builder.Build();

      using var outerScope = container.BeginScope();
      var outerInstance = outerScope.Resolve<IScopedCounter>();

      using var innerScope = container.BeginScope();
      var innerInstance = innerScope.Resolve<IScopedCounter>();
      innerInstance.Must().NotBe(outerInstance);
   }

   [DependencyInjectionContainerMatrix]
   public void after_inner_scope_is_disposed_outer_scope_instance_is_still_resolvable()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(Scoped.For<IScopedCounter>().CreatedBy(() => new ScopedCounter()));

      using var container = builder.Build();

      using var outerScope = container.BeginScope();
      var outerInstance = outerScope.Resolve<IScopedCounter>();

      {
         using var innerScope = container.BeginScope();
         innerScope.Resolve<IScopedCounter>();
      }

      var outerInstanceAfterInnerDisposed = outerScope.Resolve<IScopedCounter>();
      outerInstanceAfterInnerDisposed.Must().Be(outerInstance);
   }

   [DependencyInjectionContainerMatrix]
   public void three_levels_of_nesting_each_resolve_independent_scoped_instances()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(Scoped.For<IScopedCounter>().CreatedBy(() => new ScopedCounter()));

      using var container = builder.Build();

      using var level1Scope = container.BeginScope();
      var level1 = level1Scope.Resolve<IScopedCounter>();

      using var level2Scope = container.BeginScope();
      var level2 = level2Scope.Resolve<IScopedCounter>();

      using var level3Scope = container.BeginScope();
      var level3 = level3Scope.Resolve<IScopedCounter>();

      level1.Must().NotBe(level2);
      level2.Must().NotBe(level3);
      level1.Must().NotBe(level3);
   }

   [DependencyInjectionContainerMatrix]
   public void singletons_are_same_instance_across_nested_scopes()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         Singleton.For<IScopedService>().CreatedBy(() => new ScopedService()),
         Scoped.For<IScopedCounter>().CreatedBy(() => new ScopedCounter()));

      using var container = builder.Build();

      using var outerScope = container.BeginScope();
      var outerSingleton = outerScope.Resolve<IScopedService>();

      using var innerScope = container.BeginScope();
      var innerSingleton = innerScope.Resolve<IScopedService>();
      innerSingleton.Must().Be(outerSingleton);
   }

   [DependencyInjectionContainerMatrix]
   public void disposing_inner_scope_disposes_inner_scoped_instances_but_not_outer()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(Scoped.For<IDisposableScopedService>().CreatedBy(() => new DisposableScopedService()));

      using var container = builder.Build();

      using var outerScope = container.BeginScope();
      var outerInstance = (DisposableScopedService)outerScope.Resolve<IDisposableScopedService>();

      DisposableScopedService innerInstance;
      {
         using var innerScope = container.BeginScope();
         innerInstance = (DisposableScopedService)innerScope.Resolve<IDisposableScopedService>();
         innerInstance.IsDisposed.Must().BeFalse();
      }

      innerInstance.IsDisposed.Must().BeTrue();
      outerInstance.IsDisposed.Must().BeFalse();
   }

   [DependencyInjectionContainerMatrix]
   public void ExecuteInIsolatedScope_works_when_already_in_a_scope()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(Scoped.For<IScopedCounter>().CreatedBy(() => new ScopedCounter()));

      using var container = builder.Build();

      using var outerScope = container.BeginScope();
      var outerInstance = outerScope.Resolve<IScopedCounter>();

      var innerInstance = container.ExecuteInIsolatedScope(scope => scope.Resolve<IScopedCounter>());

      innerInstance.Must().NotBe(outerInstance);
   }
}

interface IDisposableScopedService;
class DisposableScopedService : IDisposableScopedService, IDisposable
{
   public bool IsDisposed { get; private set; }
   public void Dispose() => IsDisposed = true;
}
