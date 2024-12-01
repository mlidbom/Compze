using System;
using Compze.Functional;

namespace Compze.Persistence.EventStore.Query.Models.Generators;

interface IVersioningQueryModelGenerator : IQueryModelGenerator;

interface IVersioningQueryModelGenerator<TDocument> : IVersioningQueryModelGenerator
{
   Option<TDocument> TryGenerate(Guid id, int version);
}