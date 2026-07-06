using Compze.Teventive;
using Compze.Teventive.Taggregates.Tevents.Public;

namespace Compze.Tessaging.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels;

public abstract partial class SelfGeneratingQueryModel<TQueryModel,  TTaggregateTevent>
   where TQueryModel : SelfGeneratingQueryModel<TQueryModel,  TTaggregateTevent>
   where TTaggregateTevent : class, ITaggregateTevent
{
   public abstract partial class Component<TComponent, TComponentTevent>
      where TComponentTevent : class, TTaggregateTevent
      where TComponent : Component<TComponent, TComponentTevent>
   {
      readonly IMutableTeventDispatcher<TComponentTevent> _teventAppliersDispatcher;

      void ApplyTevent(TComponentTevent tevent) => _teventAppliersDispatcher.Dispatch(tevent);

      protected Component(TQueryModel queryModel)
         : this(
            appliersRegistrar: queryModel.RegisterTeventAppliers(),
            registerTeventAppliers: true)
      {}

      protected Component(ITeventHandlerRegistrar<TComponentTevent> appliersRegistrar, bool registerTeventAppliers, TeventDispatcherConfig? teventAppliersDispatcherConfig = null)
      {
         _teventAppliersDispatcher = IMutableTeventDispatcher<TComponentTevent>.New(teventAppliersDispatcherConfig);
         if(registerTeventAppliers)
         {
            appliersRegistrar
              .For<TComponentTevent>(ApplyTevent);
         }
      }

      protected ITeventHandlerRegistrar<TComponentTevent> RegisterTeventAppliers() => _teventAppliersDispatcher.Register();
   }
}
