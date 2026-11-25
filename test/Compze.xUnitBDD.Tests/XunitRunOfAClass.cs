using System.Collections.Concurrent;
using Xunit.Sdk;
using Xunit.v3;

namespace Compze.xUnitBDD.Tests;

///<summary>
/// A class of facts that xUnit's own class runner ran, as a test run runs a class: what xUnit reported, and what the class's<br/>
/// code recorded as it ran (<see cref="Record"/>). For specifications of what happens as xUnit runs a class that its own<br/>
/// facts can't see, such as what ends after its last fact.
///</summary>
///<remarks>
/// The classes it runs are internal, so that xUnit's own discovery skips them: only the specifications that run them do.
///</remarks>
sealed class XunitRunOfAClass
{
   static readonly AsyncLocal<ConcurrentQueue<string>?> RecordOfTheRunNow = new();

   readonly IReadOnlyList<IMessageSinkMessage> _reported;

   XunitRunOfAClass(RunSummary summary, IReadOnlyList<IMessageSinkMessage> reported, IReadOnlyList<string> recorded)
   {
      Summary = summary;
      _reported = reported;
      Recorded = recorded;
   }

   /// <summary>How many of the class's facts ran, failed and were skipped.</summary>
   public RunSummary Summary { get; }

   /// <summary>What the class's code recorded with <see cref="Record"/> as it ran, in order.</summary>
   public IReadOnlyList<string> Recorded { get; }

   /// <summary>For each fact that failed, the messages of its exception and of the exceptions inside it, one per line.</summary>
   public IReadOnlyList<string> FailuresOfTheFacts => [.. _reported.OfType<ITestFailed>().Select(MessagesOf)];

   /// <summary>The messages of the exception the class failed to clean up with, such as its fixture's disposal's, one per line.</summary>
   public IReadOnlyList<string> FailuresToCleanUpTheClass => [.. _reported.OfType<ITestClassCleanupFailure>().Select(MessagesOf)];

   /// <summary>
   /// Runs the facts of <paramref name="testClass"/> with xUnit's class runner: all of them, or only those named in<br/>
   /// <paramref name="factsAFilterPicked"/>, as a filtered run would. With <see cref="ParallelMode.All"/>, xUnit runs them in parallel.
   /// </summary>
   public static async Task<XunitRunOfAClass> Of(Type testClass, ParallelMode parallelMode = ParallelMode.None, IReadOnlyCollection<string>? factsAFilterPicked = null)
   {
      var xunitTestClass = XunitDiscovery.TestClassOf(testClass);
      var testCases = (await XunitDiscovery.TestCases(xunitTestClass)).Where(PickedByTheFilter).ToList();

      var recorded = new ConcurrentQueue<string>();
      RecordOfTheRunNow.Value = recorded;
      using var reported = new ReportedMessages();
      await using var scheduler = ExecutionScheduler.CreateUnlimited();
      await using var collectionFixtures = new FixtureMappingManager("Collection");
      using var cancellation = new CancellationTokenSource();

      var summary = await XunitTestClassRunner.Instance.Run(xunitTestClass,
                                                            testCases,
                                                            ExplicitOption.Off,
                                                            reported,
                                                            ExceptionAggregator.Create(),
                                                            cancellation,
                                                            parallelMode,
                                                            scheduler,
                                                            collectionFixtures);

      return new XunitRunOfAClass(summary, [.. reported.Messages], [.. recorded]);

      bool PickedByTheFilter(IXunitTestCase testCase) => factsAFilterPicked?.Contains(testCase.TestMethod.Method.Name) ?? true;
   }

   /// <summary>Records <paramref name="happening"/> in the run that runs the calling code, as it happens.</summary>
   public static void Record(string happening) => RecordOfTheRunThatRunsTheCallingCode().Enqueue(happening);

   /// <summary>What has been recorded so far in the run that runs the calling code, in order.</summary>
   public static IReadOnlyList<string> RecordedSoFar => [.. RecordOfTheRunThatRunsTheCallingCode()];

   static ConcurrentQueue<string> RecordOfTheRunThatRunsTheCallingCode() =>
      RecordOfTheRunNow.Value ?? throw new InvalidOperationException($"No {nameof(XunitRunOfAClass)} runs the calling code.");

   static string MessagesOf(IErrorMetadata error) => string.Join(Environment.NewLine, error.Messages);

   sealed class ReportedMessages : IMessageBus
   {
      public ConcurrentQueue<IMessageSinkMessage> Messages { get; } = new();

      public bool QueueMessage(IMessageSinkMessage message)
      {
         Messages.Enqueue(message);
         return true;
      }

      public void Dispose() {}
   }
}
