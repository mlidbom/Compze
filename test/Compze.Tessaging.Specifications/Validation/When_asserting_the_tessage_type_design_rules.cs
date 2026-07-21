using Compze.Must;
using Compze.Tessaging.TessageTypes;
using Compze.Tessaging.Validation;
using Compze.xUnitBDD;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles
#pragma warning disable CA1040 //The deliberately ill-designed fixture tessages below are empty marker interfaces on purpose
#pragma warning disable CA1812 //The fixture tessages exist as types for the rules to judge - nothing ever instantiates them

namespace Compze.Tessaging.Specifications.Validation;

///<summary>The public face of the framework's tessage-type design rules: <see cref="TessageTypeDesignRules"/> asserts, up front,
/// what the framework itself would otherwise only assert the first time a type is sent or subscribed to — including the
/// assembly-wide sweep a consumer points at the assemblies declaring their tessage vocabulary.</summary>
public class When_asserting_the_tessage_type_design_rules
{
   [XF] public void the_result_bearing_strictly_local_tommand_kind_itself_fulfills_the_rules() =>
      TessageTypeDesignRules.AssertFulfilledBy(typeof(IStrictlyLocalTommand<>));

   [XF] public void a_concrete_result_bearing_strictly_local_tommand_fulfills_the_rules() =>
      TessageTypeDesignRules.AssertFulfilledBy(typeof(ResultBearingStrictlyLocalTommand));

   [XF] public void a_type_that_is_both_a_tommand_and_a_tevent_is_refused_naming_both_kinds()
   {
      var refusal = Invoking(() => TessageTypeDesignRules.AssertFulfilledBy(typeof(ITessageThatIsBothATommandAndATevent)))
                   .Must().Throw<Exception>().Which.Message;
      refusal.Must().Contain(nameof(ITessageThatIsBothATommandAndATevent));
      refusal.Must().Contain(nameof(ITommand));
      refusal.Must().Contain(nameof(ITevent));
   }

   [XF] public void a_tessage_that_must_be_sent_transactionally_yet_is_typermedia_is_refused_pointing_at_the_strictly_local_escape()
   {
      var refusal = Invoking(() => TessageTypeDesignRules.AssertFulfilledBy(typeof(TessageThatMustBeSentTransactionallyYetIsTypermedia)))
                   .Must().Throw<Exception>().Which.Message;
      refusal.Must().Contain(nameof(TessageThatMustBeSentTransactionallyYetIsTypermedia));
      refusal.Must().Contain(nameof(IStrictlyLocalTessage));
   }

   public class sweeping_the_assemblies_declaring_a_tessage_vocabulary : When_asserting_the_tessage_type_design_rules
   {
      readonly string _sweepFailure;

      public sweeping_the_assemblies_declaring_a_tessage_vocabulary() =>
         _sweepFailure = Invoking(() => TessageTypeDesignRules.AssertFulfilledByAllTessageTypesIn(typeof(When_asserting_the_tessage_type_design_rules).Assembly))
                        .Must().Throw<Exception>().Which.Message;

      [XF] public void every_violating_type_is_reported_in_the_one_failure()
      {
         _sweepFailure.Must().Contain(nameof(ITessageThatIsBothATommandAndATevent));
         _sweepFailure.Must().Contain(nameof(TessageThatMustBeSentTransactionallyYetIsTypermedia));
      }

      [XF] public void types_fulfilling_the_rules_are_not_reported() =>
         _sweepFailure.Must().NotContain(nameof(ResultBearingStrictlyLocalTommand));
   }
}

///<summary>A concrete tommand of the result-bearing strictly local kind — the kind the rules once refused wholesale, now the
/// exemption's pin: strictly local, so requiring a transactional sender contradicts nothing.</summary>
class ResultBearingStrictlyLocalTommand : StrictlyLocal.Tommands.StrictlyLocalTommand<int>;

///<summary>Deliberately ill-designed: a tessage cannot be both an instruction to act and a statement that something happened.</summary>
interface ITessageThatIsBothATommandAndATevent : ITommand, ITevent;

///<summary>Deliberately ill-designed: a typermedia tessage may never be sent remotely from within a transaction, so requiring a
/// transactional sender without declaring the tessage strictly local is a contradiction.</summary>
class TessageThatMustBeSentTransactionallyYetIsTypermedia : ITypermediaTessage, IMustBeSentTransactionally;
