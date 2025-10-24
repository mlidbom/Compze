using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Compze.Abstractions.Public;

namespace Compze.Sql.DocumentDb.Abstractions;

public interface IDocumentDbReader : IDisposable
{
   TValue Get<TValue>(object key);
   bool TryGet<TValue>(object key, [MaybeNullWhen(false)]out TValue document);
   IEnumerable<T> GetAll<T>(IEnumerable<Guid> ids ) where T : IHasPersistentIdentity<Guid>;
}