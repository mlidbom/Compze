using System.Diagnostics.CodeAnalysis;
using Compze.Abstractions;

namespace Compze.DocumentDb;

public interface IDocumentDb
{
   bool TryGet<TDocument>(object id, Type documentType, [NotNullWhen(true)]out TDocument? value, Dictionary<Type,Dictionary<string,string>> persistentValues, bool useUpdateLock) where TDocument : class;
   void Add<TDocument>(object id, TDocument value, Dictionary<Type, Dictionary<string, string>> persistentValues) where TDocument : class;
   void Update(IEnumerable<KeyValuePair<string, object>> values, Dictionary<Type, Dictionary<string, string>> persistentValues);

   void Remove(object id, Type documentType);
   IEnumerable<TDocument> GetAll<TDocument>() where TDocument : IEntity<Guid>;
   IEnumerable<TDocument> GetAll<TDocument>(IEnumerable<Guid> ids) where TDocument : IEntity<Guid>;
   IEnumerable<Guid> GetAllIds<TDocument>() where TDocument : IEntity<Guid>;
}