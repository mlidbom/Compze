using System.Diagnostics.CodeAnalysis;
using Compze.DocumentDb.Internal.SqlLayer;
using Compze.DocumentDb.Internal.SqlLayer.Exceptions;
using Compze.DocumentDb.Public;
using Compze.Abstractions.Public;
using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Time.Public;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.CollectionsCE.GenericCE;
using Compze.Internals.SystemCE.ReflectionCE;

namespace Compze.DocumentDb.Private;

#pragma warning disable CA1724 // Type name intentionally matches namespace concept
sealed class DocumentDb : IDocumentDb
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Scoped.For<IDocumentDb>()
                                  .CreatedBy((IDocumentDbSqlLayer sqlLayer, IStructuralTypeMapper typeMapper, IDocumentDbSerializer serializer)
                                                => new DocumentDb(serializer, typeMapper, sqlLayer)));

   readonly IDocumentDbSerializer _serializer;

   readonly IStructuralTypeMapper _typeMapper;
   readonly IDocumentDbSqlLayer _sqlLayer;

   DocumentDb(IDocumentDbSerializer serializer, IStructuralTypeMapper typeMapper, IDocumentDbSqlLayer sqlLayer)
   {
      _sqlLayer = sqlLayer;
      _serializer = serializer;
      _typeMapper = typeMapper;
   }

   //todo:urgent:bug: The DocumentKey uses (id, type) as the key. But polymorphism queries by base type, while storage is by concrete type:
   // Save<Animal>("1", new Dog());
   // Save<Animal>("1", new Cat()); // Same ID, different concrete types
   // This creates two documents with the same logical ID but different type GUIDs. Tuerying Get<Animal>("1") becomes ambiguous.
   // I don't see any simple fix for this. I think we should just rip out the polymorphism support. It is too complex to manage and reason about, and the use cases are limited.
   bool IDocumentDb.TryGet<TDocument>(object id, [NotNullWhen(true)] out TDocument? value, Dictionary<Type, Dictionary<string, string>> persistentTDocuments, bool useUpdateLock) where TDocument : class
   {
      value = null;
      var idString = GetIdString(id);

      if(!_sqlLayer.TryGet(idString, AcceptableTypeIds<TDocument>(), useUpdateLock, out var readRow)) return false;

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

      _sqlLayer.Add(new IDocumentDbSqlLayer.WriteRow(id: idString, serializedDocument: serializedDocument, updateTime: UtcTimeSource.UtcNow, typeId: _typeMapper.GetMappedId(value.GetType())));

      persistentValues.GetOrAddDefault(value.GetType())[idString] = serializedDocument;
   }

   public static string GetIdString(object id) => id.ToStringNotNull().ToUpperInvariant().TrimEnd(' ');

   public void Remove(object id, Type documentType)
   {
      var rowsAffected = _sqlLayer.Remove(GetIdString(id), AcceptableTypeIds(documentType));
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
            toUpdate.Add(new IDocumentDbSqlLayer.WriteRow(item.Key, serializedDocument, now, _typeMapper.GetMappedId(item.Value.GetType())));
         }
      }

      _sqlLayer.Update(toUpdate);
   }

   IEnumerable<TDocument> IDocumentDb.GetAll<TDocument>()
   {
      var acceptableTypeIds = AcceptableTypeIds<TDocument>();

      var storedList = _sqlLayer.GetAll(acceptableTypeIds);

      return storedList.Select(Deserialize<TDocument>);
   }

   public IEnumerable<TDocument> GetAll<TDocument>(IEnumerable<Guid> ids) where TDocument : IEntity<Guid>
   {
      var storedList = _sqlLayer.GetAll(ids, AcceptableTypeIds<TDocument>());

      return storedList.Select(Deserialize<TDocument>);
   }

   public IEnumerable<Guid> GetAllIds<T>() where T : IEntity<Guid> => _sqlLayer.GetAllIds(AcceptableTypeIds<T>());

   [return: NotNull] TDocument Deserialize<TDocument>(IDocumentDbSqlLayer.ReadRow stored) =>
      (TDocument)_serializer.Deserialize(GetTypeFromId(new MappedTypeId(stored.TypeId)), stored.SerializedDocument)._assert().NotNull();

   IReadOnlySet<MappedTypeId> AcceptableTypeIds<T>() => AcceptableTypeIds(typeof(T));

   IReadOnlySet<MappedTypeId> AcceptableTypeIds(Type type) => _typeMapper.GetIdForTypesAssignableTo(type)
                                                                   .ToHashSet()
                                                                   ._assert(ids => ids.Any(), _ => $"Found no TypeIds for {type.GetFullNameCompilable()}");

   Type GetTypeFromId(MappedTypeId id) => _typeMapper.GetType(id);
}
