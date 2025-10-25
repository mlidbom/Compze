using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;

namespace Compze.Tessaging.Teventive.TeventStore.Tuery.Models.SelfGeneratingQueryModels;

public abstract partial class SelfGeneratingQueryModel<TQueryModel,  TAggregateTevent>
   where TQueryModel : SelfGeneratingQueryModel<TQueryModel,  TAggregateTevent>
   where TAggregateTevent : class, IAggregateTevent
{
   public abstract partial class Component<TComponent, TComponentTevent>
      where TComponentTevent : class, TAggregateTevent
      where TComponent : Component<TComponent, TComponentTevent>
   {
      readonly IMutableTeventDispatcher<TComponentTevent> _teventAppliersTeventDispatcher = IMutableTeventDispatcher<TComponentTevent>.New();

      void ApplyTevent(TComponentTevent @tevent) => _teventAppliersTeventDispatcher.Dispatch(@tevent);

      protected Component(TQueryModel queryModel)
         : this(
            appliersRegistrar: queryModel.RegisterTeventAppliers(),
            registerTeventAppliers: true)
      {}

      internal Component(ITeventHandlerRegistrar<TComponentTevent> appliersRegistrar, bool registerTeventAppliers)
      {
         if(registerTeventAppliers)
         {
            appliersRegistrar
              .For<TComponentTevent>(ApplyTevent);
         }
      }

      protected ITeventHandlerRegistrar<TComponentTevent> RegisterTeventAppliers() => _teventAppliersTeventDispatcher.Register();
   }
}