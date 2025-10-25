using System;
using Compze.Utilities.Functional;

namespace Compze.Abstractions.Tessaging.Teventive.EventStore.Query.Models.Generators.Public;

public interface IQueryModelGenerator;

interface IQueryModelGenerator<TDocument> : IQueryModelGenerator
{
   Option<TDocument> TryGenerate(Guid id);
}