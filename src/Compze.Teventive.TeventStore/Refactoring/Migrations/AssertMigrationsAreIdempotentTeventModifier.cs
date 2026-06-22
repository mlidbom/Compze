using Compze.Tessaging.Teventive.TeventStore.Refactoring.Migrations.Public;
using Compze.Internals.SystemCE;
using Compze.Teventive.Taggregates.Tevents.Public;

namespace Compze.Tessaging.Teventive.TeventStore.Refactoring.Migrations;

class AssertMigrationsAreIdempotentTeventModifier : ITeventModifier, IStaticInstancePropertySingleton<ITeventModifier>
{
   public static ITeventModifier Instance { get; } = new AssertMigrationsAreIdempotentTeventModifier();
   AssertMigrationsAreIdempotentTeventModifier() { }

   public void Replace(params TaggregateTevent[] tevents) => throw new NonIdempotentMigrationDetectedException();

   public void InsertBefore(params TaggregateTevent[] insert) => throw new NonIdempotentMigrationDetectedException();
}