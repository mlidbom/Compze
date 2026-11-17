using Compze.Must;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Tests.Unit.CQRS.Taggregates.SharedTomponents;
using Compze.Tests.Unit.CQRS.Taggregates.InheritingTaggregate;
using Compze.xUnitBDD;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Unit.CQRS.Taggregates.SnapshotRoundTrips;

///<summary>Findings of the Opus end review of the taggregate-store spike, shown by running code. Each passing specification shows<br/>
/// the behaviour as built, which the review says is wrong or surprising.</summary>
public static class EndReview_demonstrations
{
   ///<summary>The window in which AddRestored accepts an entity is documented as "from RestoreIdentity until its first publish". It<br/>
   /// stays open after RestoreFrom has returned: a state store hands a unit of work exactly such an instance from GetForUpdate, so code<br/>
   /// in the unit of work can add an entity without a tevent.</summary>
   public static class adding_a_restored_entity
   {
      [XF] public static void after_the_restore_has_returned_and_before_any_publish_is_accepted()
      {
         var project = ProjectTaggregate.Empty();
         project.RestoreFrom(ProjectTaggregate.Create().TakeSnapshot());

         project.Checklist.AddRestored(ChecklistItemId.New());

         project.Checklist.Count().Must().Be(1);
      }
   }

   ///<summary>Taggregate lost its protected constructor in the spike, and it isn't abstract, so the compiler gives it a public one.</summary>
   public static class the_taggregate_base_class
   {
      [XF] public static void has_a_public_parameterless_constructor() =>
         typeof(Taggregate<AnimalTaggregate, IAnimalTevent, AnimalTevent, IAnimalTevent<IAnimalTevent>, AnimalTevent<AnimalTevent>>)
           .GetConstructor(Type.EmptyTypes)!.IsPublic.Must().BeTrue();
   }
}
