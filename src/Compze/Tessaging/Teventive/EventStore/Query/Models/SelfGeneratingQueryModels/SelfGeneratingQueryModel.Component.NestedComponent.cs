using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using JetBrains.Annotations;

namespace Compze.Tessaging.Teventive.TeventStore.Tuery.Models.SelfGeneratingQueryModels;

public abstract partial class SelfGeneratingQueryModel<TQueryModel, TAggregateTevent>
   where TQueryModel : SelfGeneratingQueryModel<TQueryModel, TAggregateTevent>
   where TAggregateTevent : class, IAggregateTevent
{
   public abstract partial class Component<TComponent, TComponentTevent>
      where TComponentTevent : class, TAggregateTevent
      where TComponent : Component<TComponent, TComponentTevent>
   {
      [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
      public abstract class NestedComponent<TNestedComponent, TNestedComponentTevent> : Component<TNestedComponent, TNestedComponentTevent>
         where TNestedComponentTevent : class, TComponentTevent
         where TNestedComponent : NestedComponent<TNestedComponent, TNestedComponentTevent>
      {
         protected NestedComponent(TComponent parent) : base(parent.RegisterTeventAppliers(), registerTeventAppliers: true) {}

         protected NestedComponent(ITeventHandlerRegistrar<TNestedComponentTevent> appliersRegistrar,
                                   bool registerTeventAppliers) : base(appliersRegistrar, registerTeventAppliers) {}
      }
   }
}