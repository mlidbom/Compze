using Compze.Teventive;
using Compze.Teventive.Taggregates.Tevents.Public;
using JetBrains.Annotations;

namespace Compze.Tessaging.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels;

public abstract partial class SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>
   where TQueryModel : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>
   where TTaggregateTevent : class, ITaggregateTevent
{
   public abstract partial class Component<TComponent, TComponentTevent>
      where TComponentTevent : class, TTaggregateTevent
      where TComponent : Component<TComponent, TComponentTevent>
   {
      [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
      public abstract class NestedComponent<TNestedComponent, TNestedComponentTevent> : Component<TNestedComponent, TNestedComponentTevent>
         where TNestedComponentTevent : class, TComponentTevent
         where TNestedComponent : NestedComponent<TNestedComponent, TNestedComponentTevent>
      {
         protected NestedComponent(TComponent parent) : base(parent.RegisterTeventAppliers(), registerTeventAppliers: true) {}

         protected NestedComponent(ITeventSubscriber<TNestedComponentTevent> appliersSubscriber,
                                   bool registerTeventAppliers,
                                   TeventDispatcherConfig? teventAppliersDispatcherConfig = null) : base(appliersSubscriber, registerTeventAppliers, teventAppliersDispatcherConfig) {}
      }
   }
}
