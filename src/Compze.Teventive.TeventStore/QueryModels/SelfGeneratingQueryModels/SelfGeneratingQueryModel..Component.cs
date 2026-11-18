using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents;

namespace Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels;

public abstract partial class SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>
   where TQueryModel : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>
   where TTaggregateTevent : class, ITaggregateTevent
{
   ///<summary>A component of a query model, or of one of its components: a part with state of its own, built by applying the tevents of<br/>
   /// <typeparamref name="TComponentTevent"/>, which its parent's routing routes to it with <c>Component(...)</c>.</summary>
   public abstract class Component<TComponent, TComponentTevent> : TeventApplyingComponent<TComponent, TComponentTevent>, IParent
      where TComponentTevent : class, TTaggregateTevent
      where TComponent : Component<TComponent, TComponentTevent>
   {
      ///<summary>A component of <paramref name="parent"/>, the query model or component whose routing routes this component's tevents to it.<br/>
      /// The component keeps nothing of it: it publishes nothing, and the parent is only asserted to route to it.</summary>
#pragma warning disable CS0618 // This is just the type of infrastructure code the member is for
      protected Component(IParent parent) : base(parent, parent.TeventAppliersInternal) {}
#pragma warning restore CS0618

      ///<summary>An entity, which its collection creates.</summary>
      private protected Component() {}

#pragma warning disable CA1033 //Only a part constructed in this component reads its routing, through IParent: it should NOT clutter the component's members.
      TeventRouting IParent.TeventAppliersInternal => TeventAppliers;
#pragma warning restore CA1033
   }
}
