using Compze.Tessaging.Endpoints;
using Compze.Contracts;

namespace Compze.Tessaging.Internal.SqlLayer;

///<summary>The endpoint's prefixed table-set: the names of the tables an exactly-once endpoint's durable vertical owns in the<br/>
/// domain database it joins — its inbox, its outbox and outbox dispatching, and its durable peer memory — each prefixed with<br/>
/// the endpoint's name. The prefix is what lets any number of endpoints store side by side in one domain database; the<br/>
/// domain-level tables (the type-id interner, the tevent store, the document db) are deliberately unprefixed — they are the<br/>
/// domain's data, shared by every endpoint that joins the database.</summary>
///<remarks>The prefix makes an exactly-once endpoint's name identifier material: a letter followed by letters, digits, or<br/>
/// underscores, at most <see cref="MaximumEndpointNameLength"/> characters — asserted loud at composition, never sanitized<br/>
/// silently. The cap derives from PostgreSQL's 63-byte identifier limit and the longest identifier the schemas generate<br/>
/// (<c>IX_«name»_OutboxTessages_Unique_TessageId</c>, 35 characters beyond the name); whoever adds a longer generated<br/>
/// identifier re-derives it.</remarks>
public class EndpointTableSet
{
   ///<summary>The endpoint-name length cap: PostgreSQL's 63-byte identifier limit minus the longest identifier the schemas<br/>
   /// generate beyond the name (see the class remarks).</summary>
   public const int MaximumEndpointNameLength = 28;

   public string InboxTessages { get; }
   public string OutboxTessages { get; }
   public string OutboxTessageDispatching { get; }
   public string Peers { get; }
   public string PeerHandledTessageTypes { get; }

   EndpointTableSet(string endpointName)
   {
      InboxTessages = $"{endpointName}_InboxTessages";
      OutboxTessages = $"{endpointName}_OutboxTessages";
      OutboxTessageDispatching = $"{endpointName}_OutboxTessageDispatching";
      Peers = $"{endpointName}_Peers";
      PeerHandledTessageTypes = $"{endpointName}_PeerHandledTessageTypes";
   }

   ///<summary>The table-set <paramref name="endpoint"/> owns in the domain database it joins. Asserts that the endpoint's<br/>
   /// name is identifier material within the length cap — the storage constraint an exactly-once endpoint's name carries.</summary>
   public static EndpointTableSet For(EndpointConfiguration endpoint)
   {
      var name = endpoint.Name;
      State.Assert(IsIdentifierMaterial(name),
                   () => $"Endpoint name '{name}' cannot prefix the endpoint's table-set in the domain database it joins: an exactly-once endpoint's name is identifier material — a letter followed by letters, digits, or underscores.");
      State.Assert(name.Length <= MaximumEndpointNameLength,
                   () => $"Endpoint name '{name}' is {name.Length} characters; the cap is {MaximumEndpointNameLength}, so that every identifier the endpoint's table-set generates fits PostgreSQL's 63-byte identifier limit.");
      return new EndpointTableSet(name);
   }

   static bool IsIdentifierMaterial(string name) =>
      name.Length > 0 && char.IsAsciiLetter(name[0]) && name.All(character => char.IsAsciiLetterOrDigit(character) || character == '_');
}
