using System;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;

namespace Compze.Abstractions.Tessaging.Teventive.TeventStore.Refactoring.Migrations.Public;

///<summary>Defines an identity for migration of tevents into other tevents. Creates </summary>
public interface ITeventMigration
{
   // ReSharper disable UnusedMember.Global
   Guid Id { get; }//Todo: Complete persisting of migrations including writing this data to the store.
   string Name { get; }
   string Description { get; }
   bool Done { get; }
   // ReSharper restore UnusedMember.Global

   ///<summary>The tevent interface that is the root of the tevent hierarchy for the aggregate whose tevents this migration modifies</summary>
   Type MigratedAggregateTeventHierarchyRootInterface { get; }

   ISingleAggregateInstanceHandlingTeventMigrator CreateSingleAggregateInstanceHandlingMigrator();
}

///<summary>
/// <para>Responsible for migrating the tevents of a single instance of an aggregate.</para>
/// </summary>
public interface ISingleAggregateInstanceHandlingTeventMigrator
{
   ///<summary>
   /// <para>Inspect one tevent and if required mutate the tevent stream by calling methods on the modifier</para>
   /// <para>Called once for each tevent in the aggregate's history. </para>
   /// <para>Then it is called once with an instance of <see cref="EndOfAggregateHistoryTeventPlaceHolder"/>. </para>
   /// </summary>
   void MigrateTevent(IAggregateTevent tevent, ITeventModifier modifier);
}