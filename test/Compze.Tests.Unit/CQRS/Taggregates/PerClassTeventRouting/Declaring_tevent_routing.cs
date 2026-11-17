using Compze.Contracts.Exceptions;
using Compze.Must;
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Unit.CQRS.Taggregates.PerClassTeventRouting;

///<summary>The mistakes in declaring tevent routing that would silently lose a class's appliers, components or slots, refused loudly.</summary>
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
         Invoking(SkippingNote.ExtendTheNotesRoutingSkippingItsBaseClass).Must().Throw<ArgumentException>()
                                                                     .Which.Message.Must().Contain($"{typeof(NoteBetween).FullName} is");
   }

   public static class A_taggregate_class_that_declares_no_tevent_appliers
   {
      [XF] public static void throws_when_constructed_naming_the_class() =>
         Invoking(() => new NoteWithoutTeventAppliers()).Must().Throw<StateAssertionFailedException>()
                                                        .Which.Message.Must().Contain($"{typeof(NoteWithoutTeventAppliers).FullName} declares no tevent appliers");
   }

   public static class A_component_whose_parents_routing_has_no_entry_for_it
   {
      [XF] public static void throws_when_constructed_naming_the_component() =>
         Invoking(() => new NoteThatForgetsItsComponent()).Must().Throw<StateAssertionFailedException>()
                                                          .Which.Message.Must().Contain(typeof(ForgottenComponent).FullName!);
   }

   ///<summary>A taggregate class that implements no <see cref="ITeventRouted"/> of its own, so its base class's says it declares no tevent appliers.</summary>
   class NoteWithoutTeventAppliers : Taggregate<NoteWithoutTeventAppliers, INoteTevent, NoteTevent, INoteTevent<INoteTevent>, NoteTevent<NoteTevent>>;

   ///<summary>A note class that declares no routing of its own, and so inherits <see cref="Note"/>'s.</summary>
   class NoteWithoutARoutingOfItsOwn : Note;

   ///<summary>A note class with a component whose entry its routing forgot.</summary>
   class NoteThatForgetsItsComponent : Note, ITeventRouted
   {
      static readonly TeventRouting<NoteThatForgetsItsComponent, INoteTevent> Appliers = NoteAppliers.Extend<NoteThatForgetsItsComponent>(_ => {});
      static readonly TeventRouting<NoteThatForgetsItsComponent, INoteTevent> Handlers = NoteHandlers.Extend<NoteThatForgetsItsComponent>(_ => {});

      TeventRouting ITeventRouted.TeventAppliers => Appliers;
      TeventRouting ITeventRouted.TeventHandlers => Handlers;

      ///<summary>Constructs its component, as a note does, and is refused there: the component isn't kept.</summary>
      internal NoteThatForgetsItsComponent() => _ = new ForgottenComponent(this);
   }

   class ForgottenComponent : TeventiveComponent<NoteThatForgetsItsComponent, INoteTevent, NoteTevent, ForgottenComponent, IVocabTevent.Forms, VocabTevent.Forms>, ITeventRouted
   {
      static readonly TeventRouting<ForgottenComponent, IVocabTevent.Forms> Appliers = DeclareAppliers(_ => {});

      TeventRouting ITeventRouted.TeventAppliers => Appliers;

      public ForgottenComponent(NoteThatForgetsItsComponent note) : base(note) {}
   }

   abstract class NoteBetween : Note;

   abstract class SkippingNote : NoteBetween
   {
      internal static void ExtendTheNotesRoutingSkippingItsBaseClass() => NoteAppliers.Extend<SkippingNote>(_ => {});
   }
}
