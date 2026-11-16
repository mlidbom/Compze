namespace Compze.Teventive._internal;

///<summary>What a store that keeps taggregates as their state needs of a taggregate beyond <see cref="ITaggregate"/>: to make the<br/>
/// committed instance it serves to readers read only, to know whether a publish has failed on a copy a unit of work is changing,<br/>
/// whose state then can't be trusted, and to give a copy the version of the instance it was copied from.</summary>
interface ITaggregateStoreInternals
{
   ///<summary>From now on, publishing on this instance throws.</summary>
   void MakeReadOnly();

   ///<summary>Whether an exception has escaped a publish on this instance.</summary>
   bool APublishHasFailed { get; }

   ///<summary>Gives this instance, just restored from a snapshot, the version of the instance the snapshot was taken of, so that<br/>
   /// the versions of its tevents continue from there.</summary>
   void ContinueVersion(int version);
}
