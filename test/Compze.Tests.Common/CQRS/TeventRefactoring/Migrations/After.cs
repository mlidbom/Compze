using Compze.Tessaging.Teventive.TeventStore.Refactoring.Migrations.Public;
using Compze.Tessaging.Teventive.TeventStore.Refactoring.Migrations;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Teventive.Taggregates.Tevents.Public;

namespace Compze.Tests.Common.CQRS.TeventRefactoring.Migrations;

public class After<TTevent> : TeventMigration<ITestTaggregateTevent>
{
   readonly IEnumerable<Type> _insert;

   public static After<TTevent> Insert<T1>() => new(EnumerableCE.OfTypes<T1>());
   // ReSharper disable once UnusedMember.Global todo:Write test that uses this. We should have a test replacing with a collection.
   public static After<TTevent> Insert<T1, T2>() => new(EnumerableCE.OfTypes<T1, T2>());

   After(IEnumerable<Type> insert) : base(Guid.Parse("544C6694-7B29-4CC0-8DAA-6C50A5F28B70"), "After", "Long description of After") => _insert = insert;

   public override ISingleTaggregateInstanceHandlingTeventMigrator CreateSingleTaggregateInstanceHandlingMigrator() => new Inspector(_insert);

   class Inspector(IEnumerable<Type> insert) : ISingleTaggregateInstanceHandlingTeventMigrator
   {
      readonly IEnumerable<Type> _insert = insert;
      Type? _lastSeenTeventType;

      public void MigrateTevent(ITaggregateTevent<ITaggregateTevent> wrappedTevent, ITeventModifier modifier)
      {
         if (_lastSeenTeventType == typeof(TTevent) && wrappedTevent.Tevent.GetType() != _insert.First())
         {
            modifier.InsertBefore(_insert.ToWrappedTevents());
         }

         _lastSeenTeventType = wrappedTevent.Tevent.GetType();
      }

   }
}