using Compze.Contracts;
using Compze.Tessaging.TessageTypes;

namespace Compze.Teventive.Taggregates.BaseClasses;

///<summary>A part of a <see cref="TeventApplyingRoot{TRoot,TTevent}"/> that holds state of its own, built by applying the tevents of <typeparamref name="TComponentTevent"/>:<br/>
/// a component or an entity of a taggregate, which also publishes them, or of a query model. Its parent's <see cref="TeventRouting"/> routes those tevents to it,<br/>
/// and it routes them on to its tevent appliers, and its own parts', through the routing its class declares with <see cref="DeclareAppliers"/>.</summary>
///<remarks>Only Compze's own base classes derive from it.</remarks>
///<remarks>review-naming: a placeholder (jastudio-rep-opus, 2026-11-17), for the naming pass of the spike's end review, with <see cref="TeventApplyingRoot{TRoot,TTevent}"/>.</remarks>
public abstract class TeventApplyingComponent<TComponent, TComponentTevent> : ITeventRoutedMember<TComponentTevent>, ITeventRouted
   where TComponent : TeventApplyingComponent<TComponent, TComponentTevent>
   where TComponentTevent : class, ITevent
{
   ///<summary>A component of <paramref name="parent"/>, whose routing is asserted to route this component's tevents to a part.</summary>
   internal TeventApplyingComponent(ITeventRouted parent) : this() => TeventRouting.AssertRoutesToAPart<TComponentTevent>(parent, this);

   ///<summary>An entity of a query model, which the collection its parent's routing routes to creates: the collection is what that routing's entry<br/>
   /// is for, and it asserts the entry when it is created.</summary>
   internal TeventApplyingComponent() => TeventRouting.AssertDeclaresItsTeventAppliers(this);

#pragma warning disable CA1033 //A class that declares its tevent routing re-implements ITeventRouted; the base class's null means it declares none.
   TeventRouting? ITeventRouted.TeventAppliers => null;
   TeventRouting? ITeventRouted.TeventHandlers => null; //A component has no tevent handlers: they are declared on the taggregate.
#pragma warning restore CA1033

   ///<summary>The routing of this component's class to its tevent appliers, read through <see cref="ITeventRouted"/>: declared, as construction asserted.<br/>
   /// Its parent's routing reaches it through <see cref="TeventRouting{TInstance,TTevent}.Declaration.Component{TComponentTevent}"/>.</summary>
   TeventRouting TeventAppliers => ((ITeventRouted)this).TeventAppliers!;

   ///<summary>Declares the tevent appliers of <typeparamref name="TComponent"/>, for it to keep in a static field and return from its explicit<br/>
   /// implementation of <see cref="ITeventRouted.TeventAppliers"/>. Every tevent dispatched to the component must reach at least one of them.<br/>
   /// A subclass of <typeparamref name="TComponent"/> extends that routing with <see cref="TeventRouting{TInstance,TTevent}.Extend{TSubclass}"/>.</summary>
   protected static TeventRouting<TComponent, TComponentTevent> DeclareAppliers(Action<TeventRouting<TComponent, TComponentTevent>.Declaration> declare) =>
      TeventRouting<TComponent, TComponentTevent>.DeclareAppliers(declare);

#pragma warning disable CA1033 //These members should NOT clutter the public interface of this class.
   void ITeventRoutedMember<TComponentTevent>.DispatchInternal(IPublisherTevent<ITevent> wrapped) => TeventAppliers.Dispatch(this, wrapped);
#pragma warning restore CA1033
}
