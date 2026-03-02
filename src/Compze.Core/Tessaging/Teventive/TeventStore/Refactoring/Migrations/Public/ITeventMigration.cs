using System;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

namespace Compze.Core.Tessaging.Teventive.TeventStore.Refactoring.Migrations.Public;

///<summary>Defines an identity for migration of tevents into other tevents. Creates </summary>
public interface ITeventMigration
{
   // ReSharper disable UnusedMember.Global
   Guid Id { get; }//Todo: Complete persisting of migrations including writing this data to the store.
   string Name { get; }
   string Description { get; }
   bool Done { get; }
   // ReSharper restore UnusedMember.Global

   ///<summary>The tevent interface that is the root of the tevent hierarchy for the taggregate whose tevents this migration modifies</summary>
   Type MigratedTaggregateTeventHierarchyRootInterface { get; }

   ISingleTaggregateInstanceHandlingTeventMigrator CreateSingleTaggregateInstanceHandlingMigrator();
}

///<summary>
/// <para>Responsible for migrating the tevents of a single instance of an taggregate.</para>
/// </summary>
public interface ISingleTaggregateInstanceHandlingTeventMigrator
{
   ///<summary>
   /// <para>Inspect one tevent and if required mutate the tevent stream by calling methods on the modifier</para>
   /// <para>Called once for each tevent in the taggregate's history. </para>
   /// <para>Then it is called once with an instance of <c>EndOfTaggregateHistoryTeventPlaceHolder</c>. </para>
   /// </summary>
   void MigrateTevent(ITaggregateTevent tevent, ITeventModifier modifier);
}