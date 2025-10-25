using System;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Refactoring.Migrations.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Utilities.Contracts;

namespace Compze.Tessaging.Teventive.TeventStore.Refactoring.Migrations;

public abstract class TeventMigration<TMigratedAggregateTeventHierarchyRootInterface> : ITeventMigration
   where TMigratedAggregateTeventHierarchyRootInterface : IAggregateTevent
{
   protected TeventMigration(Guid id, string name, string description)
   {
      Assert.Argument.NotDefault(id).NotNullEmptyOrWhitespace(description).NotNullEmptyOrWhitespace(name).Is(typeof(TMigratedAggregateTeventHierarchyRootInterface).IsInterface);

      Id = id;
      Name = name;
      Description = description;
      Done = false;
   }

   public Guid Id { get; }
   public string Name { get; }
   public string Description { get; }
   public bool Done { get; }
   public Type MigratedAggregateTeventHierarchyRootInterface => typeof(TMigratedAggregateTeventHierarchyRootInterface);
   public abstract ISingleAggregateInstanceHandlingTeventMigrator CreateSingleAggregateInstanceHandlingMigrator();
}
