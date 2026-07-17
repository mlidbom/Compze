using Compze.TypeIdentifiers;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Abstractions.Hosting.Public;

///<summary>
/// What an endpoint's setup callback receives from <see cref="IEndpointHost.RegisterEndpoint"/>: the surface
/// through which everything the endpoint will be is declared, before its container is built.
///
/// The builder does not know which capabilities exist. Each capability — such as the Tessaging or Typermedia
/// pipeline — wires itself in as a <em>feature</em> through <see cref="GetOrAddFeature{TFeature}"/>, packaged
/// behind an extension method such as <c>AddExactlyOnceTessaging()</c> or <c>AddDistributedTypermedia()</c>. A feature registers its
/// services with <see cref="Registrar"/>, maps its message types with <see cref="TypeMapper"/>, schedules
/// post-container-build work with <see cref="OnContainerBuilt"/>, and adds its runtime lifecycle via
/// <see cref="AddComponent"/>. This is the seam that keeps the hosting mechanism free of knowledge of what
/// endpoints speak.
///</summary>
public interface IEndpointBuilder
{
   ///<summary>Registers the endpoint's message-type-to-identifier mappings — the contract that lets types travel between endpoints without sharing assembly-qualified names.</summary>
   ITypeMapper TypeMapper { get; }

   ///<summary>The read side of <see cref="TypeMapper"/>.</summary>
   ITypeMap TypeMap { get; }

   ///<summary>Registers components with the endpoint's container.</summary>
   IComponentRegistrar Registrar { get; }

   ///<summary>The endpoint's identity and naming; also registered in the container so the endpoint's services can know which endpoint they serve.</summary>
   EndpointConfiguration Configuration { get; }

   ///<summary>
   /// Returns the feature of type <typeparamref name="TFeature"/> already added to this endpoint, or creates it via
   /// <paramref name="createFeature"/> and remembers it. A feature wires one capability — such as the Tessaging
   /// or Typermedia pipeline — into the endpoint being built, without the builder knowing which capabilities
   /// exist. The
   /// idempotency is what lets <c>RegisterTessageHandlers</c>-style extension methods add their pipeline
   /// on first touch and reuse it afterwards.
   ///</summary>
   TFeature GetOrAddFeature<TFeature>(Func<IEndpointBuilder, TFeature> createFeature) where TFeature : class;

   ///<summary>Adds a component whose lifecycle the endpoint will drive (see <see cref="IEndpointComponent"/>). The factory runs when the endpoint starts listening, after the container is built.</summary>
   void AddComponent(Func<IRootResolver, IEndpointComponent> createComponent);

   ///<summary>Registers an action to run right after the endpoint's container has been built — for wiring that needs resolved services, such as registering discovery handlers with the resolved query executor.</summary>
   void OnContainerBuilt(Action<IRootResolver> action);
}
