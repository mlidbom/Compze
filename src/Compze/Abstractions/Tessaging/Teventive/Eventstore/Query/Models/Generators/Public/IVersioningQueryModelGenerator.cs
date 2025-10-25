using System;
using Compze.Utilities.Functional;

namespace Compze.Abstractions.Tessaging.Teventive.EventStore.Tuery.Models.Generators.Public;

interface IVersioningQueryModelGenerator : IQueryModelGenerator;

interface IVersioningQueryModelGenerator<TDocument> : IVersioningQueryModelGenerator
{
   Option<TDocument> TryGenerate(Guid id, int version);
}