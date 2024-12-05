using System;
using Compze.Contracts;

namespace Compze.Persistence.EventStore.Refactoring.Migrations;

public abstract class EventMigration<TMigratedAggregateEventHierarchyRootInterface> : IEventMigration
   where TMigratedAggregateEventHierarchyRootInterface : IAggregateEvent
{
   protected EventMigration(Guid id, string name, string description)
   {
      Assert.Argument.NotDefault(id).NotNullEmptyOrWhitespace(description).NotNullEmptyOrWhitespace(name).Is(typeof(TMigratedAggregateEventHierarchyRootInterface).IsInterface);

      Id = id;
      Name = name;
      Description = description;
      Done = false;
   }

   public Guid Id { get; }
   public string Name { get; }
   public string Description { get; }
   public bool Done { get; }
   public Type MigratedAggregateEventHierarchyRootInterface => typeof(TMigratedAggregateEventHierarchyRootInterface);
   public abstract ISingleAggregateInstanceHandlingEventMigrator CreateSingleAggregateInstanceHandlingMigrator();
}
