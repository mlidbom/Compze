using Compze.Tessaging.Endpoints.ExactlyOnce;

namespace Compze.Tessaging.TessageBus;

///<summary>Whether the tessages a handler handles keep their place in the order its exactly-once endpoint's inbox handles<br/>
/// tessages in. Chosen where the handler is registered. It applies where the inbox handles the tessage: a tessage sent or<br/>
/// published by another endpoint, and a tommand whose handler is deferred. A handler that runs in its sender's or<br/>
/// publisher's unit of work has no place in that order.</summary>
///<remarks>Admission is unchanged by it: the inbox still admits each tessage once, in the order its sender sent it. Only the<br/>
/// order of handling is given up, and only for the tessages whose handlers give it up: a sender's other tessages keep their<br/>
/// order among themselves.</remarks>
public enum HandlingOrder
{
   //review-naming: HandlingOrder, with AsAdmitted and None, as the design named them (2026-11-27).

   ///<summary>The default: handled one at a time, in the order the inbox admitted them, with every other tessage of the<br/>
   /// endpoint whose handlers keep their place. One that keeps failing holds up those behind it, until the endpoint's<br/>
   /// <see cref="RetryPolicy"/> gives up and it is marked failed.</summary>
   AsAdmitted,

   ///<summary>Handled as soon as the inbox has capacity, up to 20 tessages at once, at the same time as any other tessage,<br/>
   /// whatever was admitted before it. One that keeps failing holds up nothing. The handler then runs at the same time as the<br/>
   /// endpoint's other handlers, so what it shares with them in memory needs locking. A tevent gives up its place only when<br/>
   /// every handler it runs gives it up.</summary>
   None
}
