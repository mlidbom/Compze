using Compze.Persistence.DocumentDb;
using Compze.Persistence.EventStore;
// ReSharper disable MemberCanBeMadeStatic.Global we want composable fluent APIs. No statics please.

namespace Compze;

public class CompzeApi
{
   public EventStoreApi EventStore => new();
   public DocumentDbApi DocumentDb => new();
}