using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Compze.DDD;

namespace Compze.Persistence.DocumentDb;

public interface IDocumentDb
{
   bool TryGet<TDocument>(object id, [MaybeNullWhen(false)]out TDocument value, Dictionary<Type,Dictionary<string,string>> persistentValues, bool useUpdateLock);
   void Add<TDocument>(object id, TDocument value, Dictionary<Type, Dictionary<string, string>> persistentValues);
   void Update(IEnumerable<KeyValuePair<string, object>> values, Dictionary<Type, Dictionary<string, string>> persistentValues);

   void Remove(object id, Type documentType);
   IEnumerable<TDocument> GetAll<TDocument>() where TDocument : IHasPersistentIdentity<Guid>;
   IEnumerable<TDocument> GetAll<TDocument>(IEnumerable<Guid> ids) where TDocument : IHasPersistentIdentity<Guid>;
   IEnumerable<Guid> GetAllIds<TDocument>() where TDocument : IHasPersistentIdentity<Guid>;
}