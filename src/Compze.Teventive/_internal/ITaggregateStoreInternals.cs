namespace Compze.Teventive._internal;

///<summary>What a taggregate store needs of a taggregate beyond <see cref="ITaggregate"/>: to make an instance it serves to readers<br/>
/// read only, to know whether a publish has failed on an instance a unit of work is changing, whose state then can't be trusted,<br/>
/// and, for the store that keeps taggregates as their state, to end an instance's restore from a snapshot, and to give a copy the<br/>
/// version of the instance it was copied from.</summary>
interface ITaggregateStoreInternals
{
   ///<summary>Ends this instance's restore from a snapshot, once its restoring code has returned: its entity collections take no more<br/>
   /// entities without their created tevents.</summary>
   void EndTheRestore();

   ///<summary>From now on, publishing on this instance throws.</summary>
   void MakeReadOnly();

   ///<summary>Whether an exception has escaped a publish on this instance.</summary>
   bool APublishHasFailed { get; }

   ///<summary>Gives this instance, just restored from a snapshot, the version of the instance the snapshot was taken of, so that<br/>
   /// the versions of its tevents continue from there.</summary>
   void ContinueVersion(int version);
}
