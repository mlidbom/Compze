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
            appliersSubscriber: queryModel.RegisterTeventAppliers(),
            registerTeventAppliers: true)
      {}

      protected Component(ITeventSubscriber<TComponentTevent> appliersSubscriber, bool registerTeventAppliers, TeventDispatcherConfig? teventAppliersDispatcherConfig = null)
      {
         _teventAppliersDispatcher = IMutableTeventDispatcher<TComponentTevent>.New(teventAppliersDispatcherConfig);
         if(registerTeventAppliers)
         {
            appliersSubscriber
              .For<TComponentTevent>(ApplyTevent);
         }
      }

      protected ITeventSubscriber<TComponentTevent> RegisterTeventAppliers() => _teventAppliersDispatcher.Register();
   }
}
