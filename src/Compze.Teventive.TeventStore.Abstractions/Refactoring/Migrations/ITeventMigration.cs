using Compze.Tessaging.TessageTypes;
using Compze.Teventive.Taggregates.Tevents;

namespace Compze.Teventive.TeventStore.Abstractions.Refactoring.Migrations;

///<summary>Defines an identity for migration of tevents into other tevents. Creates </summary>
public interface ITeventMigration
{
   // ReSharper disable UnusedMember.Global
   // ReSharper disable UnusedMemberInSuper.Global
   Guid Id { get; }//Todo: Complete persisting of migrations including writing this data to the store.
   string Name { get; }
   string Description { get; }
   bool Done { get; }
   // ReSharper restore UnusedMemberInSuper.Global
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
   /// <para>Inspect one wrapped tevent and if required mutate the tevent stream by calling methods on the modifier.</para>
   /// <para>The full persisted tevent is in hand: the <see cref="ITaggregateTevent{TTeventInterface}"/> wrapper identifying the publisher, with the inner tevent in its <see cref="IPublisherTevent{TTevent}.Tevent"/>.</para>
   /// <para>Called once for each tevent in the taggregate's history. </para>
   /// <para>Then it is called once with an instance wrapping <c>EndOfTaggregateHistoryTeventPlaceHolder</c>. </para>
   /// </summary>
   void MigrateTevent(ITaggregateTevent<ITaggregateTevent> wrappedTevent, ITeventModifier modifier);
}