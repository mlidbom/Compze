using System;
using Compze.Utilities.Functional;

namespace Compze.EventStore.Query.Models.Generators;

interface IVersioningQueryModelGenerator : IQueryModelGenerator;

interface IVersioningQueryModelGenerator<TDocument> : IVersioningQueryModelGenerator
{
   Option<TDocument> TryGenerate(Guid id, int version);
}