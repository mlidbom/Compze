using System;
using Compze.Utilities.Functional;

namespace Compze.EventStore.Query.Models.Generators;

public interface IQueryModelGenerator;

interface IQueryModelGenerator<TDocument> : IQueryModelGenerator
{
   Option<TDocument> TryGenerate(Guid id);
}