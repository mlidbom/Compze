using Compze.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Tessaging.Teventive.TeventStore.Refactoring.Migrations.Public;
using Compze.Tessaging.Teventive.TeventStore.Refactoring.Migrations;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Internals.SystemCE.ReflectionCE;

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

      public void MigrateTevent(ITaggregateTevent tevent, ITeventModifier modifier)
      {
         if (tevent.GetType() == typeof(TTevent) && _lastSeenTeventType != _insert.Last())
         {
            modifier.InsertBefore(_insert.Select(Constructor.CreateInstance).Cast<TaggregateTevent>().ToArray());
         }

         _lastSeenTeventType = tevent.GetType();
      }
   }
}