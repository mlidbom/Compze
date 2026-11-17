using Compze.Abstractions;
using Compze.Contracts;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageTypes;
using Compze.Teventive.Taggregates.Tevents;

namespace Compze.Teventive.Taggregates.BaseClasses;

///<summary>The root of a tree of state built by applying a taggregate's tevents: a <see cref="Taggregate{TTaggregate,TTaggregateTevent,TTaggregateTeventImplementation,TWrapperTeventInterface,TWrapperTeventImplementation}"/>,<br/>
/// which also publishes them, or a query model, which applies the tevents a taggregate published. A tevent gives the root its id and version, and<br/>
/// reaches its tevent appliers, and its parts', through the <see cref="TeventRouting"/> its class declares with <see cref="DeclareAppliers"/>.</summary>
///<remarks>Only Compze's own base classes derive from it.</remarks>
///<remarks>review-naming: a placeholder (jastudio-rep-opus, 2026-11-17), for the naming pass of the spike's end review. Magnus: "a taggregate is not a<br/>
/// view model", so the name is for applying tevents, not for query models.</remarks>
public abstract class TeventApplyingRoot<TRoot, TTevent> : VersionedEntity<TRoot>, ITeventRouted
   where TRoot : TeventApplyingRoot<TRoot, TTevent>
   where TTevent : class, ITaggregateTevent
{
   //Yes null id passed to base. Id should be assigned by a tevent, and it should be obvious that the root is invalid until that happens. A null value never escapes the property, due to contract validation.
   internal TeventApplyingRoot() : base(null!)
   {
      Argument.Assert(typeof(TTevent).IsInterface);
      TeventRouting.AssertDeclaresItsTeventAppliers(this);
   }

#pragma warning disable CA1033 //A class that declares its tevent routing re-implements ITeventRouted; the base class's null means it declares none.
   TeventRouting? ITeventRouted.TeventAppliers => null;
   TeventRouting? ITeventRouted.TeventHandlers => null;
#pragma warning restore CA1033

   ///<summary>The routing of this root's class to its tevent appliers, read through <see cref="ITeventRouted"/>: declared, as construction asserted.</summary>
   TeventRouting TeventAppliers => ((ITeventRouted)this).TeventAppliers!;

   ///<summary>Declares the tevent appliers of <typeparamref name="TRoot"/>, for it to keep in a static field and return from its explicit<br/>
   /// implementation of <see cref="ITeventRouted.TeventAppliers"/>. Every tevent the root applies must reach at least one of them.<br/>
   /// A subclass of <typeparamref name="TRoot"/> extends that routing with <see cref="TeventRouting{TInstance,TTevent}.Extend{TSubclass}"/>.</summary>
   protected static TeventRouting<TRoot, TTevent> DeclareAppliers(Action<TeventRouting<TRoot, TTevent>.Declaration> declare) =>
      TeventRouting<TRoot, TTevent>.DeclareAppliers(declare);

   ///<summary>Applies <paramref name="wrapped"/>: a created tevent gives this root its id, every tevent its version, and then the tevent reaches<br/>
   /// the appliers its routing routes it to.</summary>
   private protected virtual void Apply(IPublisherTevent<TTevent> wrapped)
   {
      var tevent = wrapped.Tevent;
      if(tevent is ITaggregateCreatedTevent)
      {
#pragma warning disable CS0618 // Type or member is obsolete
         base.Id = tevent.TaggregateId;
#pragma warning restore CS0618 // Type or member is obsolete
      }

      Version = tevent.TaggregateVersion;
      TeventAppliers.Dispatch(this, wrapped);
   }

   ///<summary>Applies <paramref name="history"/>, in order, to this new instance, then asserts its invariants.</summary>
   private protected void ApplyHistory(IEnumerable<ITaggregateTevent<ITaggregateTevent>> history)
   {
      State.Assert(Version == 0, () => $"Only a new instance, with {nameof(Version)} 0, is loaded from a history, and this {GetType().Name} has version {Version}.");
      //The stored wrapper is applied as it stands: after a migration has rewritten history the stored wrapper is the truth, not what the publisher would wrap today.
      foreach(var wrapped in history) Apply((IPublisherTevent<TTevent>)wrapped);
      AssertInvariants();
   }

   ///<summary>Whether <paramref name="tevent"/>, published by a publisher that declares no wrapper of its own, would reach any of the appliers of this root's class.</summary>
   private protected bool HasAnApplierOf(TTevent tevent) => TeventAppliers.HasRoutesFor(PublisherTevent.WrapperTypeFor(tevent.GetType()));

   ///<summary>Asserts every invariant of this root: those of <see cref="AssertInvariantsAreMet"/>, and those its base class adds.</summary>
   private protected virtual void AssertInvariants() => AssertInvariantsAreMet();

   ///<summary>Override to assert this root's invariants: called once its history is applied, and, for a taggregate, at the end of each publish.</summary>
   protected virtual void AssertInvariantsAreMet() {}
}
