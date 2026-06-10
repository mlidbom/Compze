using Compze.TypeIdentifiers;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Abstractions.Hosting.Public;

//Todo, we should have a testing version of this that can register the current test's sql layer etc.
public interface IEndpointBuilder
{
   ITypeMapper TypeMapper { get; }
   ITypeMap TypeMap { get; }
   IComponentRegistrar Registrar { get; }
   EndpointConfiguration Configuration { get; }

   ///<summary>
   /// Returns the feature of type <typeparamref name="TFeature"/> already added to this endpoint, or creates it via
   /// <paramref name="createFeature"/> and remembers it. A feature wires one capability — such as a paradigm's
   /// pipeline — into the endpoint being built, without the builder knowing which capabilities exist.
   ///</summary>
   TFeature GetOrAddFeature<TFeature>(Func<IEndpointBuilder, TFeature> createFeature) where TFeature : class;

   ///<summary>Adds a component whose lifecycle the endpoint will drive. The factory runs when the endpoint starts listening, after the container is built.</summary>
   void AddComponent(Func<IRootResolver, IEndpointComponent> createComponent);

   ///<summary>Registers an action to run right after the endpoint's container has been built.</summary>
   void OnContainerBuilt(Action<IRootResolver> action);
}
