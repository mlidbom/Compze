using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;

namespace Compze.DependencyInjection.Specifications.Component_resolution;

interface IVocabularyService;
class VocabularyService : IVocabularyService;
interface IVocabularyPlugin;
class VocabularyPluginA : IVocabularyPlugin;
class VocabularyPluginB : IVocabularyPlugin;

///<summary>The resolution vocabulary is spoken by every resolver flavor alike: <see cref="IRootResolver"/>, a scope's
/// <see cref="IScopeResolver"/>, their base <see cref="IServiceResolver"/>, and the scope itself — each resolves single
/// components and component sets.</summary>
public class When_resolving_components_through_each_resolver_interface
{
   static IDependencyInjectionContainer CreateContainerWithAServiceAndTwoPlugins()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         Singleton.For<IVocabularyService>().CreatedBy(() => new VocabularyService()),
         Singleton.ForSet<IVocabularyPlugin>().CreatedBy(() => new VocabularyPluginA()),
         Singleton.ForSet<IVocabularyPlugin>().CreatedBy(() => new VocabularyPluginB()));
      return builder.Build();
   }

   [DependencyInjectionContainerMatrix]
   public void the_root_resolver_resolves_the_whole_component_set_with_ResolveSet()
   {
      using var container = CreateContainerWithAServiceAndTwoPlugins();
      container.RootResolver.ResolveSet<IVocabularyPlugin>().Must().HaveCount(2);
   }

   [DependencyInjectionContainerMatrix]
   public void the_base_IServiceResolver_interface_resolves_single_components_and_whole_component_sets()
   {
      using var container = CreateContainerWithAServiceAndTwoPlugins();
      IServiceResolver resolver = container.RootResolver;
      resolver.Resolve<IVocabularyService>().Must().NotBeNull();
      resolver.ResolveSet<IVocabularyPlugin>().Must().HaveCount(2);
   }

   [DependencyInjectionContainerMatrix]
   public void a_scopes_resolver_resolves_the_whole_component_set_with_ResolveSet()
   {
      using var container = CreateContainerWithAServiceAndTwoPlugins();
      using var scope = container.BeginScope();
      scope.Resolver.ResolveSet<IVocabularyPlugin>().Must().HaveCount(2);
   }

   [DependencyInjectionContainerMatrix]
   public void a_scope_resolves_a_component_by_its_runtime_Type()
   {
      using var container = CreateContainerWithAServiceAndTwoPlugins();
      using var scope = container.BeginScope();
#pragma warning disable CA2263 //The Type-taking overload IS the surface under specification: the form for component types only known at runtime.
      scope.Resolve(typeof(IVocabularyService)).Must().BeAssignableTo<IVocabularyService>();
#pragma warning restore CA2263
   }
}
