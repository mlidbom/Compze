// ReSharper disable InconsistentNaming

namespace Compze.Must.Specifications;

public class When_a_failing_assertion_shows_a_value_that_once_killed_the_test_process : UniversalTestBase
{
   const string TheScenarioPassed = "Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0";

   static string RunInAChildProcess(string scenario) => ExplicitTestInAChildProcess.Run(typeof(ScenariosThatOnceKilledTheTestProcess), scenario);

   public class that_holds_a_delegate : When_a_failing_assertion_shows_a_value_that_once_killed_the_test_process
   {
      [XF] public void the_process_survives_and_the_message_describes_the_delegate() =>
         RunInAChildProcess(nameof(ScenariosThatOnceKilledTheTestProcess.a_value_holding_a_delegate)).Must().Contain(TheScenarioPassed);
   }

   public class that_holds_a_method : When_a_failing_assertion_shows_a_value_that_once_killed_the_test_process
   {
      [XF] public void the_process_survives_and_the_message_describes_the_method() =>
         RunInAChildProcess(nameof(ScenariosThatOnceKilledTheTestProcess.a_value_holding_a_method)).Must().Contain(TheScenarioPassed);
   }

   public class that_holds_a_task_that_never_completes : When_a_failing_assertion_shows_a_value_that_once_killed_the_test_process
   {
      [XF] public void the_assertion_completes_and_the_message_shows_the_status_of_the_task() =>
         RunInAChildProcess(nameof(ScenariosThatOnceKilledTheTestProcess.a_value_holding_a_task_that_never_completes)).Must().Contain(TheScenarioPassed);
   }

   public class that_is_nested_deeper_than_Must_serializes : When_a_failing_assertion_shows_a_value_that_once_killed_the_test_process
   {
      [XF] public void the_process_survives_and_the_message_says_the_value_nests_too_deep() =>
         RunInAChildProcess(nameof(ScenariosThatOnceKilledTheTestProcess.a_value_nested_deeper_than_Must_serializes)).Must().Contain(TheScenarioPassed);
   }

   public class that_is_nested_deeper_than_Must_serializes_on_a_thread_with_a_64_megabyte_stack : When_a_failing_assertion_shows_a_value_that_once_killed_the_test_process
   {
      [XF] public void the_process_survives_and_the_message_says_the_value_nests_too_deep() =>
         RunInAChildProcess(nameof(ScenariosThatOnceKilledTheTestProcess.a_value_nested_deeper_than_Must_serializes_on_a_thread_with_a_64_megabyte_stack)).Must().Contain(TheScenarioPassed);
   }

   public class with_a_getter_that_throws_800_levels_down : When_a_failing_assertion_shows_a_value_that_once_killed_the_test_process
   {
      [XF] public void the_process_survives_and_the_message_shows_what_the_getter_threw() =>
         RunInAChildProcess(nameof(ScenariosThatOnceKilledTheTestProcess.a_value_with_a_getter_that_throws_800_levels_down)).Must().Contain(TheScenarioPassed);
   }
}
