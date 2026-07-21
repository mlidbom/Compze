using Compze.Internals.SystemCE.LinqCE;
using Compze.Teventive.Taggregates.Tevents;
using Compze.Teventive.TeventStore.Abstractions.Refactoring.Migrations;
using Compze.Teventive.TeventStore.Refactoring.Migrations;

namespace Compze.Tests.Common.CQRS.TeventRefactoring.Migrations;

public class Before<TTevent> : TeventMigration<ITestTaggregateTevent>
{
   readonly IEnumerable<Type> _insert;

   public static Before<TTevent> Insert<T1>() => new(EnumerableCE.OfTypes<T1>());
   public static Before<TTevent> Insert<T1, T2>() => new(EnumerableCE.OfTypes<T1, T2>());

   Before(IEnumerable<Type> insert) : base(Guid.Parse("0533D2E4-DE78-4751-8CAE-3343726D635B"), "Before", "Long description of Before") => _insert = insert;

   public override ISingleTaggregateInstanceHandlingTeventMigrator CreateSingleTaggregateInstanceHandlingMigrator() => new Inspector(_insert);

   class Inspector(IEnumerable<Type> insert) : ISingleTaggregateInstanceHandlingTeventMigrator
   {
      readonly IEnumerable<Type> _insert = insert;
      Type? _lastSeenTeventType;

      public void MigrateTevent(ITaggregateTevent<ITaggregateTevent> wrappedTevent, ITeventModifier modifier)
      {
         if (wrappedTevent.Tevent.GetType() == typeof(TTevent) && _lastSeenTeventType != _insert.Last())
         {
            modifier.InsertBefore(_insert.ToWrappedTevents());
         }

         _lastSeenTeventType = wrappedTevent.Tevent.GetType();
      }
   }
}