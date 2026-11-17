using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents;

namespace Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels;

public abstract partial class SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>
   where TQueryModel : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>
   where TTaggregateTevent : class, ITaggregateTevent
{
   ///<summary>A component of a query model, or of one of its components: a part with state of its own, built by applying the tevents of<br/>
   /// <typeparamref name="TComponentTevent"/>, which its parent's routing routes to it with <c>Component(...)</c>.</summary>
   public abstract class Component<TComponent, TComponentTevent> : TeventApplyingComponent<TComponent, TComponentTevent>
      where TComponentTevent : class, TTaggregateTevent
      where TComponent : Component<TComponent, TComponentTevent>
   {
      ///<summary>A component of <paramref name="parent"/>, the query model or component whose routing routes this component's tevents to it.<br/>
      /// The component keeps nothing of it: it publishes nothing, and the parent is only asserted to route to it.</summary>
      protected Component(ITeventRouted parent) : base(parent) {}

      ///<summary>An entity, which its collection creates.</summary>
      private protected Component() {}
   }
}
