using Compze.Core.Tessaging.Teventive.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

namespace Compze.Tessaging.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels;

public abstract partial class SelfGeneratingQueryModel<TQueryModel,  TTaggregateTevent>
   where TQueryModel : SelfGeneratingQueryModel<TQueryModel,  TTaggregateTevent>
   where TTaggregateTevent : class, ITaggregateTevent
{
   public abstract partial class Component<TComponent, TComponentTevent>
      where TComponentTevent : class, TTaggregateTevent
      where TComponent : Component<TComponent, TComponentTevent>
   {
      readonly IMutableTeventDispatcher<TComponentTevent> _teventAppliersTeventDispatcher = IMutableTeventDispatcher<TComponentTevent>.New();

      void ApplyTevent(TComponentTevent tevent) => _teventAppliersTeventDispatcher.Dispatch(tevent);

      protected Component(TQueryModel queryModel)
         : this(
            appliersRegistrar: queryModel.RegisterTeventAppliers(),
            registerTeventAppliers: true)
      {}

      protected Component(ITeventHandlerRegistrar<TComponentTevent> appliersRegistrar, bool registerTeventAppliers)
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