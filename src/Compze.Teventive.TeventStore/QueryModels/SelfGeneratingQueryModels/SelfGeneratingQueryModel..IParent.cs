using Compze.Abstractions;
using Compze.Teventive.Taggregates.Tevents;

namespace Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels;

public abstract partial class SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>
   where TQueryModel : SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>
   where TTaggregateTevent : class, ITaggregateTevent
{
   ///<summary>The query model, or one of its components or entities: what a component, or an entity collection, of the query model is a part of.<br/>
   /// The query model and each of its parts is one: a part's constructor, and <c>CreateSelfManagingCollection</c>, are passed <c>this</c>.<br/>
   /// Only Compze's classes implement it.</summary>
   ///<remarks>review-naming: IParent, a placeholder for the naming pass (jastudio-rep-opus's fork, 2026-11-18).</remarks>
   public interface IParent
   {
      ///<summary>The routing of this parent's class to its tevent appliers: what a part constructed in it asserts routes to it.</summary>
      [Obsolete(ObsoleteMessage.ForInternalUseOnly)] TeventRouting TeventAppliersInternal { get; }
   }

#pragma warning disable CA1033 //Only a part constructed in the query model reads its routing, through IParent: it should NOT clutter the query model's members.
   TeventRouting IParent.TeventAppliersInternal => TeventAppliers;
#pragma warning restore CA1033
}
