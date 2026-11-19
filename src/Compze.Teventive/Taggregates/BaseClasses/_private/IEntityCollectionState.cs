using System.Collections;

namespace Compze.Teventive.Taggregates.BaseClasses._private;

///<summary>A collection of entities, Compze's own, whose entities are the state of the taggregate that holds it: what the check that a snapshot<br/>
/// loses nothing compares, since it takes Compze's own types for machinery.</summary>
interface IEntityCollectionState
{
   ///<summary>Each entity, paired with its id, in the order the entities were created.</summary>
   IEnumerable EntitiesWithTheirIdsInCreationOrder { get; }
}
