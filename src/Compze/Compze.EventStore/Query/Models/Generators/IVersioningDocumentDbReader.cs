using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Compze.Abstractions;

namespace Compze.EventStore.Query.Models.Generators;

public interface IQueryModelReader
{
    TValue Get<TValue>(object key);
    bool TryGet<TValue>(object key, [MaybeNullWhen(false)] out TValue document);
    IEnumerable<T> GetAll<T>(IEnumerable<Guid> ids) where T : IHasPersistentIdentity<Guid>;
}

public interface IVersioningQueryModelReader : IQueryModelReader
{
    bool TryGetVersion<TDocument>(object key, [MaybeNullWhen(false)]out TDocument document, int version);
   TValue GetVersion<TValue>(object key, int version);
}