using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageTypes;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents;

namespace Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels;

///<summary>A query model that builds its state by applying a taggregate's tevents, exactly as the taggregate's own state is built: through the tevent appliers<br/>
/// its class declares with <see cref="TeventApplyingRoot{TRoot,TTevent}.DeclareAppliers"/>, and those of its components and entities. Unlike a taggregate,<br/>
/// it publishes nothing: the tevents are given to it, from a taggregate's history or as they are published.</summary>
public abstract partial class SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent> : TeventApplyingRoot<TQueryModel, TTaggregateTevent>,
                                                                                      SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.IParent
   where TQueryModel : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>
   where TTaggregateTevent : class, ITaggregateTevent
{
   ///<summary>Applies a tevent that arrives without a publisher-identifying wrapper - such as one delivered to an inner-typed bus subscription - by wrapping it<br/>
   /// in a <see cref="PublisherTevent{TTevent}"/> closed over its runtime type, as a dispatcher does.</summary>
   public void ApplyTevent(TTaggregateTevent tevent) => ApplyTevent(PublisherTevent.WrapTevent(tevent));

   ///<summary>Applies <paramref name="wrappedTevent"/>: a created tevent gives the query model its id, every tevent its version, and the tevent reaches its appliers.</summary>
   public void ApplyTevent(IPublisherTevent<TTaggregateTevent> wrappedTevent) => Apply(wrappedTevent);

   ///<summary>Whether <paramref name="tevent"/> would reach any applier of this query model's class: a tevent it applies nothing for changes nothing in it.</summary>
   public bool HandlesTevent(TTaggregateTevent tevent) => HasAnApplierOf(tevent);

   ///<summary>Applies <paramref name="history"/>, in order, to this new query model, then asserts its invariants.</summary>
   public void LoadFromHistory(IEnumerable<ITaggregateTevent<ITaggregateTevent>> history) => ApplyHistory(history);
}
