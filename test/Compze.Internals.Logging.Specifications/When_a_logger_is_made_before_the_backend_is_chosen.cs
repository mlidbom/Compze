// ReSharper disable InconsistentNaming

namespace Compze.Internals.Logging.Specifications;

///<summary>
/// Loggers used to bind to the backend current when they were made, so a logger made before the host chose its backend
/// wrote to the console for good. The logger here is the one <see cref="CompzeLogger.For{T}"/> shares for its type:
/// made once, on first use, so it is the logger most likely to be made before the host chooses a backend.
///</summary>
public class When_a_logger_is_made_before_the_backend_is_chosen
{
   readonly ILogger _loggerMadeFirst = CompzeLogger.For<When_a_logger_is_made_before_the_backend_is_chosen>();

   public class and_logs_after_the_backend_is_chosen : When_a_logger_is_made_before_the_backend_is_chosen
   {
      readonly CapturingLogBackend _chosenBackend;

      public and_logs_after_the_backend_is_chosen()
      {
         _chosenBackend = CapturingLogBackend.Install();
         _loggerMadeFirst.Warning("logged after the backend was chosen");
      }

      [XF] public void the_line_reaches_the_chosen_backend()
         => _chosenBackend.Captured.Select(it => it.MessageTemplate).Must().SequenceEqual(["logged after the backend was chosen"]);
   }

   public class and_logs_both_before_and_after_another_backend_is_chosen : When_a_logger_is_made_before_the_backend_is_chosen
   {
      readonly CapturingLogBackend _backendChosenFirst;
      readonly CapturingLogBackend _backendChosenSecond;

      public and_logs_both_before_and_after_another_backend_is_chosen()
      {
         _backendChosenFirst = CapturingLogBackend.Install();
         _loggerMadeFirst.Warning("logged before the second backend was chosen");
         _backendChosenSecond = CapturingLogBackend.Install();
         _loggerMadeFirst.Warning("logged after the second backend was chosen");
      }

      [XF] public void the_line_logged_before_reaches_only_the_backend_chosen_first()
         => _backendChosenFirst.Captured.Select(it => it.MessageTemplate).Must().SequenceEqual(["logged before the second backend was chosen"]);

      [XF] public void the_line_logged_after_reaches_only_the_backend_chosen_second()
         => _backendChosenSecond.Captured.Select(it => it.MessageTemplate).Must().SequenceEqual(["logged after the second backend was chosen"]);
   }
}
