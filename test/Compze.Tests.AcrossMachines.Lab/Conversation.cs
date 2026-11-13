using System.Collections.Concurrent;
using System.Diagnostics;
using Compze.DependencyInjection.Microsoft;
using Compze.Serialization.Newtonsoft.Wiring;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Tessaging.Transport.Tcp;
using Compze.Tessaging.Typermedia;
using Compze.Tessaging.Typermedia.Client;
using Compze.TypeIdentifiers.DependencyInjection;

namespace Compze.Tests.AcrossMachines.Lab;

///<summary>A conversation with the lab endpoint (<see cref="LabEndpoint"/>) from another machine: a pure client<br/>
/// (<see cref="TypermediaClient"/>) over TCP asks <see cref="TueryAskedFromAnotherMachine"/> round after round, and reports<br/>
/// what each tuery experienced, one line each: answered, by which machine and how fast, or failed, after how long and with<br/>
/// what. What the lab is for is read from those lines: what a conversation experiences while the endpoint's machine is<br/>
/// there, when it vanishes, and when it comes back.</summary>
sealed class Conversation
{
   readonly EndpointAddress _labEndpointAddress;
   readonly ConversationOptions _options;
   readonly ConcurrentDictionary<string, Exception> _firstFailureOfEachKind = new();

   public Conversation(EndpointAddress labEndpointAddress, ConversationOptions options)
   {
      _labEndpointAddress = labEndpointAddress;
      _options = options;
   }

   ///<summary>Connects to the lab endpoint, then asks round after round until the conversation's duration is over.</summary>
   public async Task HoldAsync()
   {
      await using var client = TypermediaClient.Build(new MicrosoftContainerBuilder(),
                                                      builder =>
                                                      {
                                                         builder.ConfigureTransport(registrar => registrar.TcpEndpointTransportClient())
                                                                .DeclareRequiredTypeMappings(registrar => registrar.RequireMappedTypesFromAssemblyContaining<TueryAskedFromAnotherMachine>());
                                                         builder.NewtonsoftSerializer();
                                                      });

      var connecting = Stopwatch.StartNew();
      await client.ConnectAsync(_labEndpointAddress);
      Report($"connected to {_labEndpointAddress} in {MillisecondsOf(connecting)} ms");

      var navigator = client.Navigator;
      var held = Stopwatch.StartNew();
      for(var round = 1; _options.Duration is not {} duration || held.Elapsed < duration; round++)
      {
         var thisRound = round;
         await Task.WhenAll(Enumerable.Range(1, _options.TueriesPerRound).Select(numberInRound => AskAndReportAsync(navigator, $"{thisRound}.{numberInRound}")));
         await Task.Delay(_options.PauseBetweenRounds);
      }
   }

   async Task AskAndReportAsync(IRemoteTypermediaNavigator navigator, string tueryNumber)
   {
      var asked = Stopwatch.StartNew();
      try
      {
         var answer = await navigator.GetAsync(new TueryAskedFromAnotherMachine(_options.AnswerPayloadCharacters, _options.AnswerAfter));
         Report($"tuery {tueryNumber} answered by {answer.AnsweredByMachine} in {MillisecondsOf(asked)} ms, carrying {answer.Payload.Length} characters");
      }
#pragma warning disable CA1031 // Whatever a tuery fails with is what the lab exists to observe: it is reported, and the conversation goes on to see what the next tuery experiences.
      catch(Exception failure)
#pragma warning restore CA1031
      {
         Report($"tuery {tueryNumber} FAILED after {MillisecondsOf(asked)} ms: {OneLineOf(failure)}");
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
      if(_firstFailureOfEachKind.TryAdd(OneLineOf(failure), failure)) Report($"the first failure of that kind, in full:{Environment.NewLine}{failure}");
   }

   static string MillisecondsOf(Stopwatch stopwatch) => stopwatch.Elapsed.TotalMilliseconds.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);

   static void Report(string whatHappened) => Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {whatHappened}");
}
