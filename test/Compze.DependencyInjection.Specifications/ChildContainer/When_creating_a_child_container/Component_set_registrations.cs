using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;


namespace Compze.DependencyInjection.Specifications.ChildContainer.When_creating_a_child_container;

interface IPlugin;
class PluginA : IPlugin;
class PluginB : IPlugin;
class DisposablePlugin : IPlugin, IDisposable
{
   public bool IsDisposed { get; private set; }
   public void Dispose() => IsDisposed = true;
}

public class Component_set_registrations
{
   [DependencyInjectionContainerMatrix]
   public void child_resolves_the_same_singleton_set_member_instances_as_the_parent()
   {
      var parentBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      parentBuilder.Registrar.Register(
         Singleton.ForSet<IPlugin>().CreatedBy(() => new PluginA()),
         Singleton.ForSet<IPlugin>().CreatedBy(() => new PluginB()));

      using var parent = parentBuilder.Build();
      using var child = parent.CreateChildContainerBuilder().Build();

      var parentInstances = parent.ResolveSet<IPlugin>().ToArray();
      var childInstances = child.ResolveSet<IPlugin>().ToArray();

      childInstances.Must().HaveCount(2);
      childInstances.ToHashSet().SetEquals(parentInstances).Must().BeTrue();
   }

   [DependencyInjectionContainerMatrix]
   public void disposing_the_child_does_not_dispose_the_parents_singleton_set_members()
   {
      var parentBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      parentBuilder.Registrar.Register(Singleton.ForSet<IPlugin>().CreatedBy(() => new DisposablePlugin()));

      using var parent = parentBuilder.Build();
      var child = parent.CreateChildContainerBuilder().Build();

      var parentInstance = (DisposablePlugin)parent.ResolveSet<IPlugin>().Single();
      var childInstance = (DisposablePlugin)child.ResolveSet<IPlugin>().Single();
      childInstance.Must().Be(parentInstance);

      child.Dispose();

      parentInstance.IsDisposed.Must().BeFalse();
   }

   [DependencyInjectionContainerMatrix]
   public void scoped_set_members_resolve_fresh_instances_in_a_child_scope()
   {
      var parentBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      parentBuilder.Registrar.Register(
         Scoped.ForSet<IPlugin>().CreatedBy(() => new PluginA()),
         Scoped.ForSet<IPlugin>().CreatedBy(() => new PluginB()));

      using var parent = parentBuilder.Build();
      using var child = parent.CreateChildContainerBuilder().Build();

      using var parentScope = parent.BeginScope();
      using var childScope = child.BeginScope();

      var parentInstances = parentScope.ResolveSet<IPlugin>().ToArray();
      var childInstances = childScope.ResolveSet<IPlugin>().ToArray();

      childInstances.Must().HaveCount(2);
      childInstances.Intersect(parentInstances).Must().BeEmpty();
   }

   [DependencyInjectionContainerMatrix]
   public void creating_a_child_container_from_a_set_mixing_singleton_and_scoped_members_throws_InvalidOperationException()
   {
      var parentBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      parentBuilder.Registrar.Register(
         Singleton.ForSet<IPlugin>().CreatedBy(() => new PluginA()),
         Scoped.ForSet<IPlugin>().CreatedBy(() => new PluginB()));

      using var parent = parentBuilder.Build();

      parent.Invoking(it => it.CreateChildContainerBuilder())
            .Must()
            .Throw<InvalidOperationException>()
            .Which.Message.Must()
            .Contain("IPlugin");
   }
}
