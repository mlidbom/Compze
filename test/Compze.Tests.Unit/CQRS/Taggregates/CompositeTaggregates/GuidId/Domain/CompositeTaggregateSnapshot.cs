using Compze.Abstractions;

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain;

///<summary>A composite taggregate's whole state: its own, its component's, and its entities', each with its nested entities. It doesn't override<br/>
/// Equals: the comparison of two is <see cref="HoldTheSameState"/>, given with the class.</summary>
class CompositeTaggregateSnapshot
{
   public CompositeTaggregateSnapshot(TaggregateId id, string name, ComponentSnapshot component, IReadOnlyList<EntitySnapshot> entities)
   {
      Id = id;
      Name = name;
      Component = component;
      Entities = entities;
   }

   public TaggregateId Id { get; }
   public string Name { get; }
   public ComponentSnapshot Component { get; }
   public IReadOnlyList<EntitySnapshot> Entities { get; }

   public static bool HoldTheSameState(CompositeTaggregateSnapshot one, CompositeTaggregateSnapshot other) => one.Description == other.Description;

   string Description => $"{Id} {Name} [{Component.Description}] [{string.Join("; ", Entities.Select(it => it.Description))}]";
}

///<summary>The state of a composite taggregate's component: its name, its two collections of entities, and its nested component's name.</summary>
class ComponentSnapshot
{
   public ComponentSnapshot(string name, IReadOnlyList<NamedEntitySnapshot> entities, IReadOnlyList<NamedEntitySnapshot> nonRemovableEntities, string nestedComponentName)
   {
      Name = name;
      Entities = entities;
      NonRemovableEntities = nonRemovableEntities;
      NestedComponentName = nestedComponentName;
   }

   public string Name { get; }
   public IReadOnlyList<NamedEntitySnapshot> Entities { get; }
   public IReadOnlyList<NamedEntitySnapshot> NonRemovableEntities { get; }
   public string NestedComponentName { get; }

   internal string Description => $"{Name} [{string.Join("; ", Entities.Select(it => it.Description))}] [{string.Join("; ", NonRemovableEntities.Select(it => it.Description))}] {NestedComponentName}";
}

///<summary>The state of an entity at a composite taggregate's root: its id, its name, and its nested entities'.</summary>
class EntitySnapshot
{
   public EntitySnapshot(Guid id, string name, IReadOnlyList<NamedEntitySnapshot> nestedEntities)
   {
      Id = id;
      Name = name;
      NestedEntities = nestedEntities;
   }

   public Guid Id { get; }
   public string Name { get; }
   public IReadOnlyList<NamedEntitySnapshot> NestedEntities { get; }

   internal string Description => $"{Id} {Name} [{string.Join("; ", NestedEntities.Select(it => it.Description))}]";
}

///<summary>The state of an entity with no parts of its own: its id and its name.</summary>
class NamedEntitySnapshot
{
   public NamedEntitySnapshot(Guid id, string name)
   {
      Id = id;
      Name = name;
   }

   public Guid Id { get; }
   public string Name { get; }

   internal string Description => $"{Id} {Name}";
}
