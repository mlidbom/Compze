using Compze.Tessaging.TessageTypes;

namespace Compze.Teventive.Taggregates.BaseClasses;

///<summary>A part of a <see cref="TeventApplyingRoot{TRoot,TTevent}"/> that holds state of its own, built by applying the tevents of <typeparamref name="TPartTevent"/>:<br/>
/// a component or an entity of a taggregate, which also publishes them, or of a query model. Its parent's <see cref="TeventRouting"/> routes those tevents to it,<br/>
/// and it routes them on to its tevent appliers, and its own parts', through the routing its class declares with <see cref="DeclareAppliers"/>.</summary>
///<remarks>Only Compze's own base classes derive from it.</remarks>
public abstract class TeventApplyingPart<TPart, TPartTevent> : ITeventRoutedPart<TPartTevent>
   where TPart : TeventApplyingPart<TPart, TPartTevent>
   where TPartTevent : class, ITevent
{
   ///<summary>A part of <paramref name="parent"/>, whose routing, <paramref name="parentAppliers"/>, is asserted to route this part's<br/>
   /// tevents to a part.</summary>
   internal TeventApplyingPart(object parent, TeventRouting parentAppliers) : this() => parentAppliers.AssertRoutesToAPart<TPartTevent>(parent, this);

   ///<summary>An entity of a query model, which the collection its parent's routing routes to creates: the collection is what that routing's entry<br/>
   /// is for, and it asserts the entry when it is created.</summary>
   internal TeventApplyingPart()
   {
      // ReSharper disable once VirtualMemberCallInConstructor The routing is a table in a static field, and the assertion that the class declares its own must run at construction.
      TeventRouting.AssertDeclaresItsTeventAppliers(this, TeventAppliers);
   }

   ///<summary>The routing of this part's class to its tevent appliers: the table the class declares with <see cref="DeclareAppliers"/>, or a<br/>
   /// subclass with <see cref="TeventRouting{TInstance,TTevent}.Extend{TSubclass}"/>, keeps in a static field, and returns from its override:<br/>
   /// <c>protected override TeventRouting TeventAppliers => Appliers;</c><br/>
   /// Every class overrides it with its own: one that inherits its base class's is refused when it is constructed. Its parent's routing<br/>
   /// reaches it through <see cref="TeventRouting{TInstance,TTevent}.Declaration.Component{TComponentTevent}"/>, or, for an entity, through the<br/>
   /// collection that <see cref="TeventRouting{TInstance,TTevent}.Declaration.Entities{TEntityTevent}"/> routes to.</summary>
   protected abstract TeventRouting TeventAppliers { get; }

   ///<summary>Declares the tevent appliers of <typeparamref name="TPart"/>, for it to keep in a static field and return from its override of<br/>
   /// <see cref="TeventAppliers"/>. Every tevent dispatched to the part must reach at least one of them.<br/>
   /// A tevent reaches every route it matches, one after another, in the order <paramref name="declare"/> declares them.<br/>
   /// A subclass of <typeparamref name="TPart"/> extends that routing with <see cref="TeventRouting{TInstance,TTevent}.Extend{TSubclass}"/>, whose routes come after these.</summary>
   protected static TeventRouting<TPart, TPartTevent> DeclareAppliers(Action<TeventRouting<TPart, TPartTevent>.Declaration> declare) =>
      TeventRouting<TPart, TPartTevent>.DeclareAppliers(declare);

#pragma warning disable CA1033 //These members should NOT clutter the public interface of this class.
   void ITeventRoutedPart<TPartTevent>.DispatchInternal(IPublisherTevent<ITevent> wrapped) => TeventAppliers.Dispatch(this, wrapped);
#pragma warning restore CA1033
}
