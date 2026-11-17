namespace Compze.Teventive.Taggregates.BaseClasses._private;

///<summary>A taggregate, or a part of one, that can tell whether its taggregate is being restored from a snapshot: what lets an entity collection<br/>
/// add a restored entity without a tevent, then and only then.</summary>
interface ITeventiveInATaggregate
{
   ///<summary>Whether the taggregate is being restored from a snapshot: from its <c>RestoreIdentity</c> until its first publish, and never once it<br/>
   /// is a committed instance, which is read only.</summary>
   bool ItsTaggregateIsBeingRestored { get; }
}
