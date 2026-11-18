using Compze.Contracts.Exceptions;
using Compze.Must;
using Compze.Teventive;
using Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Unit.CQRS.Taggregates.PerClassTeventRouting;

///<summary>A query model routes the tevents it is given through the tevent routing its class declares, as a taggregate does.</summary>
public class Given_a_query_model_of_vocab_notes
{
   readonly VocabQueryModel _queryModel = new();

   [XF] public void it_handles_a_renaming_which_its_routing_has_an_applier_of() => _queryModel.HandlesTevent(new NoteTevent.Renamed("taberu")).Must().BeTrue();
   [XF] public void it_does_not_handle_an_archiving_which_its_routing_lets_go_unhandled() => _queryModel.HandlesTevent(new NoteTevent.Archived()).Must().BeFalse();
   [XF] public void it_does_not_handle_a_count_of_forms_which_its_routing_has_no_applier_of() => _queryModel.HandlesTevent(new VocabTevent.Forms.Counted(2)).Must().BeFalse();

   [XF] public void applying_a_count_of_forms_it_was_asked_about_still_throws_TeventUnhandledException()
   {
      _queryModel.HandlesTevent(new VocabTevent.Forms.Counted(2));
      Invoking(() => _queryModel.ApplyTevent(new VocabTevent.Forms.Counted(2))).Must().Throw<TeventUnhandledException>();
   }

   public class after_it_applies_a_renaming : Given_a_query_model_of_vocab_notes
   {
      public after_it_applies_a_renaming() => _queryModel.ApplyTevent(new NoteTevent.Renamed("taberu"));

      [XF] public void it_has_the_new_name() => _queryModel.Name.Must().Be("taberu");
   }

   public class a_query_model_whose_routing_has_no_entry_for_its_component
   {
      [XF] public void throws_when_constructed_naming_the_component() =>
         Invoking(() => new VocabQueryModelThatForgetsItsComponent()).Must().Throw<StateAssertionFailedException>()
                                                                  .Which.Message.Must().Contain(typeof(VocabQueryModelThatForgetsItsComponent.FormsComponent).FullName!);
   }

   class VocabQueryModel : SelfGeneratingQueryModel<VocabQueryModel, INoteTevent>
   {
      static readonly TeventRouting<VocabQueryModel, INoteTevent> Appliers = DeclareAppliers(it => it
        .For<INoteTevent.Renamed>(static (vocab, tevent) => vocab.Name = tevent.Name)
        .IgnoreUnhandled<INoteTevent.Archived>());

      protected override TeventRouting TeventAppliers => Appliers;

      public string Name { get; private set; } = "";
   }

   class VocabQueryModelThatForgetsItsComponent : SelfGeneratingQueryModel<VocabQueryModelThatForgetsItsComponent, INoteTevent>
   {
      static readonly TeventRouting<VocabQueryModelThatForgetsItsComponent, INoteTevent> Appliers = DeclareAppliers(_ => {});

      protected override TeventRouting TeventAppliers => Appliers;

      ///<summary>Constructs its component, as a query model does, and is refused there: the component isn't kept.</summary>
      public VocabQueryModelThatForgetsItsComponent() => _ = new FormsComponent(this);

      internal class FormsComponent : Component<FormsComponent, IVocabTevent.Forms>
      {
         static readonly TeventRouting<FormsComponent, IVocabTevent.Forms> FormsAppliers = DeclareAppliers(_ => {});

         protected override TeventRouting TeventAppliers => FormsAppliers;

         public FormsComponent(VocabQueryModelThatForgetsItsComponent parent) : base(parent) {}
      }
   }
}
