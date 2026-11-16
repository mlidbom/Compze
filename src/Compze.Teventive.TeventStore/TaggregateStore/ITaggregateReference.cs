using Compze.Abstractions;

namespace Compze.Teventive.TaggregateStore;

///<summary>Stands for one taggregate of a taggregate store for as long as it exists: what code holds across units of work,<br/>
/// never an instance. An instance is one version of the taggregate's state, and is replaced at every commit that changes it.</summary>
public interface ITaggregateReference<out TTaggregate> where TTaggregate : class, ISnapshotTaggregate
{
   ///<summary>The id of the taggregate this reference stands for.</summary>
   TaggregateId Id { get; }

   ///<summary>Inside a unit of work that holds a copy of the taggregate, that copy; otherwise the committed instance.</summary>
   TTaggregate Current { get; }

   ///<summary>The ambient unit of work's copy of the taggregate, which it may change. Throws when there is no ambient unit of work.</summary>
   TTaggregate ForUpdate();
}
