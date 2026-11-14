using System.Collections.Concurrent;
using System.Diagnostics;
using Compze.Tessaging.Typermedia;

namespace Compze.Tests.AcrossMachines.Lab;

///<summary>A conversation with the lab endpoint (<see cref="LabEndpoint"/>) from another machine: asks<br/>
/// <see cref="TueryAskedFromAnotherMachine"/> round after round, and reports what each tuery experienced, one line each:<br/>
/// answered, by which machine and how fast, or failed, after how long and with what. What the lab is for is read from<br/>
/// those lines: what a conversation experiences while the endpoint's machine is there, when it vanishes, and when it<br/>
/// comes back.</summary>
///<remarks>Who asks decides how the lab endpoint is found: a pure client that was told its address<br/>
/// (<see cref="ConversingPureClient"/>), or an endpoint that finds it through the lab registry (<see cref="ConversingEndpoint"/>).</remarks>
sealed class Conversation
{
   readonly IRemoteTypermediaNavigator _navigator;
   readonly ConversationOptions _options;
   readonly ConcurrentDictionary<string, Exception> _firstFailureOfEachKind = new();

   public Conversation(IRemoteTypermediaNavigator navigator, ConversationOptions options)
   {
      _navigator = navigator;
      _options = options;
   }

   ///<summary>Asks round after round until the conversation's duration is over.</summary>
   public async Task HoldAsync()
   {
      var held = Stopwatch.StartNew();
      for(var round = 1; _options.Duration is not {} duration || held.Elapsed < duration; round++)
      {
         var thisRound = round;
         await Task.WhenAll(Enumerable.Range(1, _options.TueriesPerRound).Select(numberInRound => AskAndReportAsync($"{thisRound}.{numberInRound}")));
         await Task.Delay(_options.PauseBetweenRounds);
      }
   }

   async Task AskAndReportAsync(string tueryNumber)
   {
      var asked = Stopwatch.StartNew();
      try
      {
         var answer = await _navigator.GetAsync(new TueryAskedFromAnotherMachine(_options.AnswerPayloadCharacters, _options.AnswerAfter));
         LabReport.Line($"tuery {tueryNumber} answered by {answer.AnsweredByMachine} in {LabReport.MillisecondsOf(asked)} ms, carrying {answer.Payload.Length} characters");
      }
#pragma warning disable CA1031 // Whatever a tuery fails with is what the lab exists to observe: it is reported, and the conversation goes on to see what the next tuery experiences.
      catch(Exception failure)
#pragma warning restore CA1031
      {
         LabReport.Line($"tuery {tueryNumber} FAILED after {LabReport.MillisecondsOf(asked)} ms: {OneLineOf(failure)}");
         ReportInFullTheFirstTime(failure);
      }
   }

   ///<summary>The failure and what caused it, innermost last, each as its type and the first line of its message.</summary>
   static string OneLineOf(Exception failure)
   {
      var failureAndItsCauses = new List<string>();
      for(var current = failure; current != null; current = current.InnerException)
         failureAndItsCauses.Add($"{current.GetType().Name}: {current.Message.ReplaceLineEndings("\n").Split('\n')[0]}");
      return string.Join(" <- ", failureAndItsCauses);
   }

   void ReportInFullTheFirstTime(Exception failure)
   {
      if(_firstFailureOfEachKind.TryAdd(OneLineOf(failure), failure)) LabReport.Line($"the first failure of that kind, in full:{Environment.NewLine}{failure}");
   }
}
