using System.Diagnostics.CodeAnalysis;
using Compze.Persistence.DocumentDb;

namespace Compze.Persistence.EventStore.Query.Models.Generators;

interface IVersioningDocumentDbReader : IDocumentDbReader
{
   bool TryGetVersion<TDocument>(object key, [MaybeNullWhen(false)]out TDocument document, int version);
   TValue GetVersion<TValue>(object key, int version);
}