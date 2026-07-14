using Compze.Tests.Common.CQRS.TeventRefactoring.Migrations;
using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.Teventive.TeventStore.Abstractions.Refactoring.Migrations.Public;
using Compze.Teventive.TeventStore.Refactoring.Migrations;

namespace Compze.Tests.Integration.CQRS.TeventRefactoring.Migrations;

class Replace<TTevent> : TeventMigration<ITestTaggregateTevent>
{
   readonly Migrator _migratorSingleton;

   public static Replace<TTevent> With<T1>() => new(EnumerableCE.OfTypes<T1>());
   public static Replace<TTevent> With<T1, T2>() => new(EnumerableCE.OfTypes<T1, T2>());

   Replace(IEnumerable<Type> replaceWith) : base(Guid.Parse("9B51F7BC-D9B3-43C7-A183-76CA5E662091"), "Replace", "Long description of Replace") => _migratorSingleton = new Migrator(replaceWith);

   public override ISingleTaggregateInstanceHandlingTeventMigrator CreateSingleTaggregateInstanceHandlingMigrator() => _migratorSingleton;

   class Migrator(IEnumerable<Type> replaceWith) : ISingleTaggregateInstanceHandlingTeventMigrator
   {
      readonly IEnumerable<Type> _replaceWith = replaceWith;

      public void MigrateTevent(ITaggregateTevent<ITaggregateTevent> wrappedTevent, ITeventModifier modifier)
      {
         if (wrappedTevent.Tevent.GetType() == typeof(TTevent))
         {
            modifier.Replace(_replaceWith.ToWrappedTevents());
         }
      }
   }
}
