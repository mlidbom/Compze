using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Compze.Abstractions;
using Compze.Abstractions.Internal;
using Compze.Abstractions.Internal.Persistence.DocumentDb;
using Compze.Abstractions.Internal.Refactoring.Naming;
using Compze.Abstractions.Internal.Time;
using Compze.Persistence.DocumentDb.Abstractions;
using Compze.Serialization;
using Compze.Utilities.Contracts;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE.CollectionsCE.GenericCE;
using Compze.Utilities.SystemCE.ReflectionCE;

namespace Compze.Persistence.DocumentDb;

class DocumentDb : IDocumentDb
{
   readonly IUtcTimeTimeSource _timeSource;
   readonly IDocumentDbSerializer _serializer;

   readonly ITypeMapper _typeMapper;
   readonly IDocumentDbPersistenceLayer _persistenceLayer;

   internal DocumentDb(IUtcTimeTimeSource timeSource, IDocumentDbSerializer serializer, ITypeMapper typeMapper, IDocumentDbPersistenceLayer persistenceLayer)
   {
      _persistenceLayer = persistenceLayer;
      _timeSource = timeSource;
      _serializer = serializer;
      _typeMapper = typeMapper;
   }


    //todo:urgent:bug: The DocumentKey uses (id, type) as the key. But polymorphism queries by base type, while storage is by concrete type:
    // Save<Animal>("1", new Dog());
    // Save<Animal>("1", new Cat()); // Same ID, different concrete types
    // This creates two documents with the same logical ID but different type GUIDs. Querying Get<Animal>("1") becomes ambiguous.
    // I don't see any simple fix for this. I think we should just rip out the polymorphism support. It is too complex to manage and reason about, and the use cases are limited.
    bool IDocumentDb.TryGet<TDocument>(object id, [MaybeNullWhen(false)] out TDocument value, Dictionary<Type, Dictionary<string, string>> persistentTDocuments, bool useUpdateLock)
   {
      value = default;
      var idString = GetIdString(id);

      if(!_persistenceLayer.TryGet(idString, AcceptableTypeIds<TDocument>(), useUpdateLock, out var readRow)) return false;

      var found = Deserialize<TDocument>(readRow);

      //Things such as TimeZone etc can cause roundtripping serialization to result in different values from the original so don't cache the read string. Cache the result of serializing it again.
      //performance: Try to find a way to remove the need to do this so that we can get rid of the overhead of an extra serialization.
      persistentTDocuments.GetOrAddDefault(found.GetType())[idString] = _serializer.Serialize(found);

      value = found;
      return true;
   }

   public void Add<TDocument>(object id, TDocument value, Dictionary<Type, Dictionary<string, string>> persistentValues)
   {
      Assert.Argument.NotNull(value);

      var idString = GetIdString(id);
      var serializedDocument = _serializer.Serialize(value);

      _persistenceLayer.Add(new IDocumentDbPersistenceLayer.WriteRow(id: idString, serializedDocument:  serializedDocument, updateTime: _timeSource.UtcNow, typeId: _typeMapper.GetId(value.GetType()).GuidValue));

      persistentValues.GetOrAddDefault(value.GetType())[idString] = serializedDocument;
   }

   internal static string GetIdString(object id) => id.ToStringNotNull().ToUpperInvariant().TrimEnd(' ');

   public void Remove(object id, Type documentType)
   {
      var rowsAffected = _persistenceLayer.Remove(GetIdString(id), AcceptableTypeIds(documentType));
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

      var toUpdate = new List<IDocumentDbPersistenceLayer.WriteRow>();
      var now = _timeSource.UtcNow;
      foreach(var item in values)
      {
         var serializedDocument = _serializer.Serialize(item.Value);
         var needsUpdate = !persistentValues.GetOrAddDefault(item.Value.GetType()).TryGetValue(item.Key, out var oldValue) || serializedDocument != oldValue;
         if(needsUpdate)
         {
            persistentValues.GetOrAddDefault(item.Value.GetType())[item.Key] = serializedDocument;
            toUpdate.Add(new IDocumentDbPersistenceLayer.WriteRow(item.Key, serializedDocument, now, _typeMapper.GetId(item.Value.GetType()).GuidValue));
         }
      }

      _persistenceLayer.Update(toUpdate);
   }

   IEnumerable<TDocument> IDocumentDb.GetAll<TDocument>()
   {
      var acceptableTypeIds = AcceptableTypeIds<TDocument>();

      var storedList = _persistenceLayer.GetAll(acceptableTypeIds);

      return storedList.Select(Deserialize<TDocument>);
   }

   public IEnumerable<TDocument> GetAll<TDocument>(IEnumerable<Guid> ids) where TDocument : IHasPersistentIdentity<Guid>
   {
      var storedList = _persistenceLayer.GetAll(ids, AcceptableTypeIds<TDocument>());

      return storedList.Select(Deserialize<TDocument>);
   }

   public IEnumerable<Guid> GetAllIds<T>() where T : IHasPersistentIdentity<Guid> => _persistenceLayer.GetAllIds(AcceptableTypeIds<T>());


   [return:NotNull]TDocument Deserialize<TDocument>(IDocumentDbPersistenceLayer.ReadRow stored) =>
      (TDocument)Assert.Result.ReturnNotNull(_serializer.Deserialize(GetTypeFromId(new TypeId(stored.TypeId)), stored.SerializedDocument));

   IReadOnlySet<Guid> AcceptableTypeIds<T>() => AcceptableTypeIds(typeof(T));
   IReadOnlySet<Guid> AcceptableTypeIds(Type type) => _typeMapper.GetIdForTypesAssignableTo(type)
                                                                 .Select(typeId => typeId.GuidValue)
                                                                 .ToHashSet()
                                                                 .assert(ids => ids.Any(), it => $"Found no TypeIds for {type.GetFullNameCompilable()}");

   Type GetTypeFromId(TypeId id) => _typeMapper.GetType(id);
}