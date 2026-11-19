using Compze.Internals.SystemCE;
using Compze.Teventive.Taggregates.Tevents;
using Compze.Teventive.TeventStore.Abstractions.Refactoring.Migrations;

namespace Compze.Teventive.TeventStore.Refactoring.Migrations._private;

class AssertMigrationsAreIdempotentTeventModifier : ITeventModifier, IStaticInstancePropertySingleton<ITeventModifier>
{
   public static ITeventModifier Instance { get; } = new AssertMigrationsAreIdempotentTeventModifier();
   AssertMigrationsAreIdempotentTeventModifier() { }

   public void Replace(params ITaggregateTevent<ITaggregateTeventWithIdentity>[] wrappedTevents) => throw new NonIdempotentMigrationDetectedException();

   public void InsertBefore(params ITaggregateTevent<ITaggregateTeventWithIdentity>[] insert) => throw new NonIdempotentMigrationDetectedException();
}