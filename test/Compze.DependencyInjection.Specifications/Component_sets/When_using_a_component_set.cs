using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;

namespace Compze.DependencyInjection.Specifications.Component_sets;

interface IPlugin;
class PluginA : IPlugin;
class PluginB : IPlugin;

public class When_using_a_component_set
{
   [DependencyInjectionContainerMatrix]
   public void Singleton_ForSet_members_are_all_returned_by_ResolveSet()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         Singleton.ForSet<IPlugin>().CreatedBy(() => new PluginA()),
         Singleton.ForSet<IPlugin>().CreatedBy(() => new PluginB()));

      using var container = builder.Build();

      var plugins = container.ResolveSet<IPlugin>().ToArray();

      plugins.Must().HaveCount(2);
      plugins.OfType<PluginA>().Any().Must().BeTrue();
      plugins.OfType<PluginB>().Any().Must().BeTrue();
   }

   [DependencyInjectionContainerMatrix]
   public void ResolveSet_for_an_unregistered_component_set_type_returns_empty()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      using var container = builder.Build();

      container.ResolveSet<IPlugin>().Must().BeEmpty();
   }

   [DependencyInjectionContainerMatrix]
   public void Resolving_a_component_set_type_singularly_throws_InvalidOperationException()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(Singleton.ForSet<IPlugin>().CreatedBy(() => new PluginA()));

      using var container = builder.Build();

      container.Invoking(it => it.Resolve<IPlugin>())
               .Must()
               .Throw<InvalidOperationException>()
               .Which.Message.Must()
               .Contain("IPlugin")
               .Contain("ResolveSet");
   }

   [DependencyInjectionContainerMatrix]
   public void Resolving_a_singular_service_via_ResolveSet_throws_InvalidOperationException()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(Singleton.For<IPlugin>().CreatedBy(() => new PluginA()));

      using var container = builder.Build();

      container.Invoking(it => it.ResolveSet<IPlugin>().ToArray())
               .Must()
               .Throw<InvalidOperationException>()
               .Which.Message.Must()
               .Contain("IPlugin")
               .Contain("Resolve");
   }

   [DependencyInjectionContainerMatrix]
   public void A_singleton_independently_resolvable_by_its_own_type_can_also_join_a_component_set_via_composition()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         Singleton.For<PluginA>().CreatedBy(() => new PluginA()),
         Singleton.ForSet<IPlugin>().CreatedBy((PluginA actual) => actual));

      using var container = builder.Build();

      var resolvedDirectly = container.Resolve<PluginA>();
      var resolvedFromSet = container.ResolveSet<IPlugin>().Single();

      resolvedFromSet.Must().Be(resolvedDirectly);
   }

   [DependencyInjectionContainerMatrix]
   public void Scoped_ForSet_members_are_all_returned_by_ResolveSet_within_the_same_scope()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         Scoped.ForSet<IPlugin>().CreatedBy(() => new PluginA()),
         Scoped.ForSet<IPlugin>().CreatedBy(() => new PluginB()));

      using var container = builder.Build();
      using var scope = container.BeginScope();

      scope.ResolveSet<IPlugin>().ToArray().Must().HaveCount(2);
   }
}
