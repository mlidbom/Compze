using System.Diagnostics.CodeAnalysis;
using Compze.DocumentDb._internal.SqlLayer;
using Compze.DocumentDb.Exceptions;
using Compze.DocumentDb._private.SqlLayer.Exceptions;
using Compze.DocumentDb.Public;
using Compze.Abstractions.Public;
using Compze.TypeIdentifiers;
using Compze.Abstractions.Serialization._internal;
using Compze.Abstractions.Time.Public;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.CollectionsCE.GenericCE;

namespace Compze.DocumentDb._private;

#pragma warning disable CA1724 // Type name intentionally matches namespace concept
sealed class DocumentDb : IDocumentDb
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Scoped.For<IDocumentDb>()
                                  .CreatedBy((IDocumentDbSqlLayer sqlLayer, ITypeMap typeMap, IDocumentDbSerializer serializer)
                                                => new DocumentDb(serializer, typeMap, sqlLayer)));

   readonly IDocumentDbSerializer _serializer;

   readonly ITypeMap _typeMap;
   readonly IDocumentDbSqlLayer _sqlLayer;

   DocumentDb(IDocumentDbSerializer serializer, ITypeMap typeMap, IDocumentDbSqlLayer sqlLayer)
   {
      _sqlLayer = sqlLayer;
      _serializer = serializer;
      _typeMap = typeMap;
   }

   // A document is identified by (id, concrete type): the same id may hold one document per concrete type. The query
   // therefore filters by the concrete document type, never by the declared TDocument (which may be a base type the
   // caller used at the call site). TDocument is only the type the result is returned as.
   bool IDocumentDb.TryGet<TDocument>(object id, Type documentType, [NotNullWhen(true)] out TDocument? value, Dictionary<Type, Dictionary<string, string>> persistentTDocuments, bool useUpdateLock) where TDocument : class
   {
      value = null;
      var idString = GetIdString(id);

      if(!_sqlLayer.TryGet(idString, _typeMap.GetId(documentType), useUpdateLock, out var readRow)) return false;

      var found = Deserialize<TDocument>(readRow);

      //Things such as TimeZone etc can cause roundtripping serialization to result in different values from the original so don't cache the read string. Cache the result of serializing it again.
      //performance: Try to find a way to remove the need to do this so that we can get rid of the overhead of an extra serialization.
      persistentTDocuments.GetOrAddDefault(found.GetType())[idString] = _serializer.Serialize(found);

      value = found;
      return true;
   }

   public void Add<TDocument>(object id, TDocument value, Dictionary<Type, Dictionary<string, string>> persistentValues) where TDocument : class
   {
      Argument.Assert(value is not null);

      var idString = GetIdString(id);
      var serializedDocument = _serializer.Serialize(value);

      _sqlLayer.Add(new IDocumentDbSqlLayer.WriteRow(id: idString, serializedDocument: serializedDocument, updateTime: UtcTimeSource.UtcNow, typeId: _typeMap.GetId(value.GetType())));

      persistentValues.GetOrAddDefault(value.GetType())[idString] = serializedDocument;
   }

   public static string GetIdString(object id) => id.ToStringNotNull().ToUpperInvariant().TrimEnd(' ');

   public void Remove(object id, Type documentType)
   {
      var rowsAffected = _sqlLayer.Remove(GetIdString(id), _typeMap.GetId(documentType));
#pragma warning disable IDE0010
      switch(rowsAffected)
      {
         case < 1:
            throw new NoSuchDocumentException(id, documentType);
         case > 1:
            throw new TooManyItemsDeletedException();
      }
#pragma warning restore IDE0010
   }

   public void Update(IEnumerable<KeyValuePair<string, object>> values, Dictionary<Type, Dictionary<string, string>> persistentValues)
   {
      values = values.ToList();

      var toUpdate = new List<IDocumentDbSqlLayer.WriteRow>();
      var now = UtcTimeSource.UtcNow;
      foreach(var item in values)
      {
         var serializedDocument = _serializer.Serialize(item.Value);
         var needsUpdate = !persistentValues.GetOrAddDefault(item.Value.GetType()).TryGetValue(item.Key, out var oldValue) || serializedDocument != oldValue;
         if(needsUpdate)
         {
            persistentValues.GetOrAddDefault(item.Value.GetType())[item.Key] = serializedDocument;
            toUpdate.Add(new IDocumentDbSqlLayer.WriteRow(item.Key, serializedDocument, now, _typeMap.GetId(item.Value.GetType())));
         }
      }

      _sqlLayer.Update(toUpdate);
   }

   IEnumerable<TDocument> IDocumentDb.GetAll<TDocument>()
   {
      var storedList = _sqlLayer.GetAll(_typeMap.GetId(typeof(TDocument)));

      return storedList.Select(Deserialize<TDocument>);
   }

   public IEnumerable<TDocument> GetAll<TDocument>(IEnumerable<Guid> ids) where TDocument : IEntity<Guid>
   {
      var storedList = _sqlLayer.GetAll(ids, _typeMap.GetId(typeof(TDocument)));

      return storedList.Select(Deserialize<TDocument>);
   }

   public IEnumerable<Guid> GetAllIds<T>() where T : IEntity<Guid> => _sqlLayer.GetAllIds(_typeMap.GetId(typeof(T)));

   [return: NotNull] TDocument Deserialize<TDocument>(IDocumentDbSqlLayer.ReadRow stored) =>
      (TDocument)_serializer.Deserialize(stored.TypeId.Type, stored.SerializedDocument)._assert().NotNull();
}
