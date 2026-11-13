using System.Globalization;

namespace Compze.Tests.AcrossMachines.Lab;

///<summary>How a <see cref="Conversation"/> is to be held: how long, how many tueries it asks at once in each round, how long<br/>
/// it pauses between rounds, and what answer each tuery orders (<see cref="TueryAskedFromAnotherMachine"/>).</summary>
sealed class ConversationOptions
{
   ///<summary>How long the conversation goes on. Null: until the process is killed.</summary>
   public TimeSpan? Duration { get; private set; }

   public TimeSpan PauseBetweenRounds { get; private set; } = TimeSpan.FromMilliseconds(500);

   ///<summary>How many tueries each round asks at once. More than one makes the client hold that many connections.</summary>
   public int TueriesPerRound { get; private set; } = 1;

   public int AnswerPayloadCharacters { get; private set; }

   public TimeSpan AnswerAfter { get; private set; } = TimeSpan.Zero;

   ///<summary>Reads the options from the command line's <c>--name value</c> pairs that follow the lab endpoint's address.</summary>
   public static ConversationOptions Parse(string[] namesAndValues)
   {
      if(namesAndValues.Length % 2 != 0) throw new ArgumentException($"Every option takes one value. Got: {string.Join(' ', namesAndValues)}");

      var options = new ConversationOptions();
      foreach(var nameAndValue in namesAndValues.Chunk(2))
      {
         var (name, value) = (nameAndValue[0], int.Parse(nameAndValue[1], CultureInfo.InvariantCulture));
         switch(name)
         {
            case "--for":                       options.Duration = TimeSpan.FromSeconds(value); break;
            case "--pause-between-rounds":      options.PauseBetweenRounds = TimeSpan.FromMilliseconds(value); break;
            case "--tueries-per-round":         options.TueriesPerRound = value; break;
            case "--answer-payload-characters": options.AnswerPayloadCharacters = value; break;
            case "--answer-after":              options.AnswerAfter = TimeSpan.FromMilliseconds(value); break;
            default:                            throw new ArgumentException($"Unknown option: {name}");
         }
      }
      return options;
   }
}
