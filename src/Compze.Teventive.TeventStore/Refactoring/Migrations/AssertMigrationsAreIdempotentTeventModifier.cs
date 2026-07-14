using Compze.Internals.SystemCE;
using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.Teventive.TeventStore.Abstractions.Refactoring.Migrations.Public;

namespace Compze.Teventive.TeventStore.Refactoring.Migrations;

class AssertMigrationsAreIdempotentTeventModifier : ITeventModifier, IStaticInstancePropertySingleton<ITeventModifier>
{
   public static ITeventModifier Instance { get; } = new AssertMigrationsAreIdempotentTeventModifier();
   AssertMigrationsAreIdempotentTeventModifier() { }

   public void Replace(params ITaggregateTevent<ITaggregateTevent>[] wrappedTevents) => throw new NonIdempotentMigrationDetectedException();

   public void InsertBefore(params ITaggregateTevent<ITaggregateTevent>[] insert) => throw new NonIdempotentMigrationDetectedException();
}