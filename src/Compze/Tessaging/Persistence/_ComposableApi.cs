using Compze.Tessaging.Persistence.DocumentDb;
using Compze.Tessaging.Persistence.EventStore;

// ReSharper disable MemberCanBeMadeStatic.Global we want composable fluent APIs. No statics please.

namespace Compze.Tessaging.Persistence;

public class CompzeApi
{
   public EventStoreApi EventStore => new();
   public DocumentDbApi DocumentDb => new();
}