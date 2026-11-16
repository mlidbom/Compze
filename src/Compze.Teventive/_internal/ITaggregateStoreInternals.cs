namespace Compze.Teventive._internal;

///<summary>What a taggregate store needs of a taggregate beyond <see cref="ITaggregate"/>: to make the instance it serves to readers<br/>
/// read only, and to know whether a publish has failed on an instance a unit of work is changing, whose state can then not be trusted.</summary>
interface ITaggregateStoreInternals
{
   ///<summary>From now on, publishing on this instance throws.</summary>
   void MakeReadOnly();

   ///<summary>Whether an exception has escaped a publish on this instance.</summary>
   bool APublishHasFailed { get; }
}
