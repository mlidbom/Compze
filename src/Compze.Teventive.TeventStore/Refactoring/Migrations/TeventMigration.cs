using Compze.Tessaging.Teventive.TeventStore.Refactoring.Migrations.Public;
using Compze.Contracts;
using Compze.Teventive.Public.Taggregates.Tevents.Public;

namespace Compze.Tessaging.Teventive.TeventStore.Refactoring.Migrations;

public abstract class TeventMigration<TMigratedTaggregateTeventHierarchyRootInterface> : ITeventMigration
   where TMigratedTaggregateTeventHierarchyRootInterface : ITaggregateTevent
{
   protected TeventMigration(Guid id, string name, string description)
   {
      Contract.Argument.NotDefault(id).NotNullEmptyOrWhitespace(description).NotNullEmptyOrWhitespace(name).Assert(typeof(TMigratedTaggregateTeventHierarchyRootInterface).IsInterface);

      Id = id;
      Name = name;
      Description = description;
      Done = false;
   }

   public Guid Id { get; }
   public string Name { get; }
   public string Description { get; }
   public bool Done { get; }
   public Type MigratedTaggregateTeventHierarchyRootInterface => typeof(TMigratedTaggregateTeventHierarchyRootInterface);
   public abstract ISingleTaggregateInstanceHandlingTeventMigrator CreateSingleTaggregateInstanceHandlingMigrator();
}
