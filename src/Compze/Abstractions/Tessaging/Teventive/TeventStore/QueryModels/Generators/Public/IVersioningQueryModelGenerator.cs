using System;
using Compze.Utilities.Functional;

namespace Compze.Core.Tessaging.Teventive.TeventStore.QueryModels.Generators.Public;

interface IVersioningQueryModelGenerator : IQueryModelGenerator;

interface IVersioningQueryModelGenerator<TDocument> : IVersioningQueryModelGenerator
{
   Option<TDocument> TryGenerate(Guid id, int version);
}