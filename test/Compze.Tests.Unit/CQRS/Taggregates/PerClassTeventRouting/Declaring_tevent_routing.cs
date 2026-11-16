using Compze.Must;
using Compze.Teventive;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Unit.CQRS.Taggregates.PerClassTeventRouting;

///<summary>The two mistakes in declaring tevent routing that would silently lose a class's appliers, components or slots, refused loudly.</summary>
public static class Declaring_tevent_routing
{
   public static class A_note_class_that_inherits_its_base_classes_routing_without_declaring_its_own
   {
      [XF] public static void throws_when_constructed_naming_both_classes() =>
         Invoking(() => new NoteWithoutARoutingOfItsOwn()).Must().Throw<Exception>()
                                                          .Which.Message.Must().Contain($"{typeof(NoteWithoutARoutingOfItsOwn).FullName} uses the tevent routing declared for {typeof(Note).FullName}");
   }

   public static class Extending_the_routing_of_a_class_that_is_not_the_direct_base_class
   {
      [XF] public static void throws_naming_the_direct_base_class() =>
         Invoking(() => TeventRouting<Animal>.DeclareAppliers(_ => {}).Extend<Dog>(_ => {})).Must().Throw<ArgumentException>()
                                                                                             .Which.Message.Must().Contain($"{typeof(Mammal).FullName} is");
   }

   ///<summary>A note class that declares no routing of its own, and so inherits <see cref="Note"/>'s.</summary>
   class NoteWithoutARoutingOfItsOwn : Note;

   abstract class Animal;
   abstract class Mammal : Animal;
   abstract class Dog : Mammal;
}
