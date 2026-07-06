using Compze.Abstractions.Public;
using Compze.Contracts;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Teventive;
using Compze.Teventive.Taggregates.Tevents.Public;

namespace Compze.Tessaging.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels;

public partial class SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent> : VersionedEntity<TQueryModel>
   where TQueryModel : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>
   where TTaggregateTevent : class, ITaggregateTevent
{
   //Yes null id passed to base. Id should be assigned by an action, and it should be obvious that the taggregate in invalid until that happens
   protected SelfGeneratingQueryModel(TeventDispatcherConfig? teventAppliersDispatcherConfig = null) : base(null!)
   {
      Contract.Argument.Assert(typeof(TTaggregateTevent).IsInterface);
      _teventAppliersDispatcher = IMutableTeventDispatcher<TTaggregateTevent>.New(teventAppliersDispatcherConfig);
   }

   readonly IMutableTeventDispatcher<TTaggregateTevent> _teventAppliersDispatcher;

   protected ITeventSubscriber<TTaggregateTevent> RegisterTeventAppliers() => _teventAppliersDispatcher.Register();

   public void ApplyTevent(TTaggregateTevent theTevent)
   {
      if(theTevent is ITaggregateCreatedTevent)
      {
#pragma warning disable CS0618 // Type or member is obsolete
         Id = theTevent.TaggregateId;
#pragma warning restore CS0618 // Type or member is obsolete
      }

      Version = theTevent.TaggregateVersion;
      _teventAppliersDispatcher.Dispatch(theTevent);
   }

   public bool HandlesTevent(TTaggregateTevent tevent) => _teventAppliersDispatcher.Handles(tevent);

   public void LoadFromHistory(IEnumerable<ITaggregateTevent> history)
   {
      Contract.State.Assert(Version == 0);
      history.ForEach(theTevent => ApplyTevent((TTaggregateTevent)theTevent));
      AssertInvariantsAreMet();
   }

   protected virtual void AssertInvariantsAreMet(){}
}
