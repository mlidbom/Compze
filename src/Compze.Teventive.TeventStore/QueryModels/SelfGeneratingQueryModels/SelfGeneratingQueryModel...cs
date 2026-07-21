using Compze.Abstractions;
using Compze.Contracts;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageTypes;
using Compze.Teventive.Taggregates.Tevents;

namespace Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels;

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

   ///<summary>Applies a tevent that arrives without a publisher-identifying wrapper - such as one delivered to an inner-typed bus subscription - by wrapping it<br/>
   /// in a <see cref="PublisherTevent{TTevent}"/> closed over its runtime type, mirroring <see cref="ITeventDispatcher{TTevent}.Dispatch(TTevent)"/>.</summary>
   public void ApplyTevent(TTaggregateTevent tevent) => ApplyTevent(PublisherTevent.WrapTevent(tevent));

   public void ApplyTevent(IPublisherTevent<TTaggregateTevent> wrappedTevent)
   {
      if(wrappedTevent.Tevent is ITaggregateCreatedTevent)
      {
#pragma warning disable CS0618 // Type or member is obsolete
         Id = wrappedTevent.Tevent.TaggregateId;
#pragma warning restore CS0618 // Type or member is obsolete
      }

      Version = wrappedTevent.Tevent.TaggregateVersion;
      _teventAppliersDispatcher.Dispatch(wrappedTevent);
   }

   public bool HandlesTevent(TTaggregateTevent tevent) => _teventAppliersDispatcher.Handles(tevent);

   public void LoadFromHistory(IEnumerable<ITaggregateTevent<ITaggregateTevent>> history)
   {
      Contract.State.Assert(Version == 0);
      history.ForEach(wrappedTevent => ApplyTevent((IPublisherTevent<TTaggregateTevent>)wrappedTevent));
      AssertInvariantsAreMet();
   }

   protected virtual void AssertInvariantsAreMet(){}
}
