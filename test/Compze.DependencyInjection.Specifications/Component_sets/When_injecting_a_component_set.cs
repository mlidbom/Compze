using System.Collections;
using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;

namespace Compze.DependencyInjection.Specifications.Component_sets;

class PluginConsumer
{
   public IComponentSet<IPlugin> Plugins { get; }
   public PluginConsumer(IComponentSet<IPlugin> plugins) => Plugins = plugins;
}

class HandWrittenPluginSet : IComponentSet<IPlugin>
{
   public IEnumerator<IPlugin> GetEnumerator() => Enumerable.Empty<IPlugin>().GetEnumerator();
   IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class When_injecting_a_component_set
{
   [DependencyInjectionContainerMatrix]
   public void a_CreatedBy_dependency_on_IComponentSet_receives_every_singleton_set_member()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         Singleton.ForSet<IPlugin>().CreatedBy(() => new PluginA()),
         Singleton.ForSet<IPlugin>().CreatedBy(() => new PluginB()),
         Singleton.For<PluginConsumer>().CreatedBy((IComponentSet<IPlugin> plugins) => new PluginConsumer(plugins)));

      using var container = builder.Build();

      var receivedPlugins = container.Resolve<PluginConsumer>().Plugins.ToArray();

      receivedPlugins.Must().HaveCount(2);
      receivedPlugins.ToHashSet().SetEquals(container.ResolveSet<IPlugin>()).Must().BeTrue();
   }

   [DependencyInjectionContainerMatrix]
   public void a_scoped_consumer_of_a_scoped_member_set_receives_its_own_scopes_instances()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         Scoped.ForSet<IPlugin>().CreatedBy(() => new PluginA()),
         Scoped.ForSet<IPlugin>().CreatedBy(() => new PluginB()),
         Scoped.For<PluginConsumer>().CreatedBy((IComponentSet<IPlugin> plugins) => new PluginConsumer(plugins)));

      using var container = builder.Build();
      using var firstScope = container.BeginScope();
      using var secondScope = container.BeginScope();

      var firstScopesPlugins = firstScope.Resolve<PluginConsumer>().Plugins.ToArray();
      var secondScopesPlugins = secondScope.Resolve<PluginConsumer>().Plugins.ToArray();

      firstScopesPlugins.ToHashSet().SetEquals(firstScope.ResolveSet<IPlugin>()).Must().BeTrue();
      firstScopesPlugins.Intersect(secondScopesPlugins).Must().BeEmpty();
   }

   [DependencyInjectionContainerMatrix]
   public void building_with_a_singleton_depending_on_a_set_of_scoped_members_throws_InvalidLifeStyleCombinationException()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         Scoped.ForSet<IPlugin>().CreatedBy(() => new PluginA()),
         Singleton.For<PluginConsumer>().CreatedBy((IComponentSet<IPlugin> plugins) => new PluginConsumer(plugins)));

      builder.Invoking(it => it.Build())
             .Must()
             .Throw<InvalidLifeStyleCombinationException>();
   }

   [DependencyInjectionContainerMatrix]
   public void registering_IComponentSet_by_hand_throws_InvalidOperationException()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();

      builder.Registrar
             .Invoking(it => it.Register(Singleton.For<IComponentSet<IPlugin>>().CreatedBy(() => new HandWrittenPluginSet())))
             .Must()
             .Throw<InvalidOperationException>()
             .Which.Message.Must()
             .Contain("ForSet");
   }

   [DependencyInjectionContainerMatrix]
   public void a_CreatedBy_dependency_on_the_set_member_type_itself_is_rejected_recommending_IComponentSet()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         Singleton.ForSet<IPlugin>().CreatedBy(() => new PluginA()),
         Singleton.For<object>().CreatedBy((IPlugin plugin) => new object()));

      builder.Invoking(it => it.Build())
             .Must()
             .Throw<InvalidOperationException>()
             .Which.Message.Must()
             .Contain("IComponentSet");
   }

   [DependencyInjectionContainerMatrix]
   public void a_component_registered_in_a_child_container_receives_the_parents_singleton_set_members()
   {
      var parentBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      parentBuilder.Registrar.Register(
         Singleton.ForSet<IPlugin>().CreatedBy(() => new PluginA()),
         Singleton.ForSet<IPlugin>().CreatedBy(() => new PluginB()));

      using var parent = parentBuilder.Build();

      var childBuilder = parent.CreateChildContainerBuilder();
      childBuilder.Registrar.Register(Singleton.For<PluginConsumer>().CreatedBy((IComponentSet<IPlugin> plugins) => new PluginConsumer(plugins)));
      using var child = childBuilder.Build();

      child.Resolve<PluginConsumer>().Plugins.ToHashSet().SetEquals(parent.ResolveSet<IPlugin>()).Must().BeTrue();
   }
}
