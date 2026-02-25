using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Compze.Core.DocumentDb.Infrastructure;
using Compze.Core.DocumentDb.Internal.SqlLayer.Exceptions;
using Compze.Core.DocumentDb.Public;
using Compze.Core.Public;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.SystemCE.TransactionsCE;
using Compze.Utilities.SystemCE.UsageGuards;
using Compze.Contracts;
using static Compze.Contracts.Assert;

namespace Compze.Core.DocumentDb.Private;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public partial class DocumentDbSession : IDocumentDbSession
{
   public static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(Scoped.For<IDocumentDbSession, IDocumentDbUpdater, IDocumentDbReader, IDocumentDbBulkReader>()
                               .CreatedBy((IDocumentDb documentDb) => new ContextEnsuringWrapper(new DocumentDbSession(documentDb))));

   public class ContextEnsuringWrapper : IDocumentDbSession
   {
      readonly UsageGuard<DocumentDbSession> _guarded;

      public ContextEnsuringWrapper(DocumentDbSession wrapped)
      {
         _guarded = new UsageGuard<DocumentDbSession>(wrapped,
                                                      new CombinationUsageGuard(
                                                         new SingleThreadUseGuard(wrapped),
                                                         new SingleTransactionUsageGuard(wrapped),
                                                         new EnlistInAmbientTransactionUsageGuard(() => _guarded!.Wrapped.FlushChanges())));
      }

      DocumentDbSession Guarded => _guarded.Wrapped;

      public void Dispose() => Guarded.Dispose();

      public TValue Get<TValue>(object key) => Guarded.Get<TValue>(key);

      public bool TryGet<TValue>(object key, [MaybeNullWhen(false)] out TValue document) => Guarded.TryGet(key, out document);

      public IEnumerable<T> GetAll<T>(IEnumerable<EntityId<Guid>> ids) where T : IEntity<Guid> => Guarded.GetAll<T>(ids);

      public IEnumerable<T> GetAll<T>() where T : IEntity<Guid> => Guarded.GetAll<T>();

      public IEnumerable<Guid> GetAllIds<T>() where T : IEntity<Guid> => Guarded.GetAllIds<T>();

      public TValue GetForUpdate<TValue>(object key) => Guarded.GetForUpdate<TValue>(key);

      public void Save<TValue>(object id, TValue value) => Guarded.Save(id, value);

      public void Delete<TEntity>(object id) => Guarded.Delete<TEntity>(id);

      public void Save<TEntity>(TEntity entity) where TEntity : IEntity<Guid> => Guarded.Save(entity);

      public void Delete<TEntity>(TEntity entity) where TEntity : IEntity<Guid> => Guarded.Delete(entity);
   }

   readonly EntitiesByIdAndTypeCache _entitiesByIdAndType = new();

   readonly IDocumentDb _backingStore;

   readonly IDictionary<DocumentKey, DocumentItem> _handledDocuments = new Dictionary<DocumentKey, DocumentItem>();

   DocumentDbSession(IDocumentDb backingStore) => _backingStore = backingStore;

   public virtual bool TryGet<TValue>(object key, [MaybeNullWhen(false)] out TValue document) => TryGetInternal(key, typeof(TValue), out document, useUpdateLock: false);

   bool TryGetInternal<TValue>(object key, Type documentType, [MaybeNullWhen(false)] out TValue value, bool useUpdateLock)
   {
      if(documentType.IsInterface)
      {
         throw new ArgumentException("You cannot tuery by id for an interface type. There is no guarantee of uniqueness");
      }

      if(_entitiesByIdAndType.TryGet(key, out value) && documentType.IsInstanceOfType(value))
      {
         return true;
      }

      var documentItem = GetDocumentItem(key, documentType);
      if(!documentItem.IsDeleted && _backingStore.TryGet(key, out value, _persistentValues, useUpdateLock) && documentType.IsInstanceOfType(value))
      {
         OnInitialLoad(key, value);
         return true;
      }

      return false;
   }

   DocumentItem GetDocumentItem(object key, Type documentType)
   {
      var documentKey = new DocumentKey(key, documentType);
      if(_handledDocuments.TryGetValue(documentKey, out var doc)) return doc;

      doc = new DocumentItem(documentKey, _backingStore, _persistentValues);
      _handledDocuments.Add(documentKey, doc);

      return doc;
   }

   void OnInitialLoad(object key, object value)
   {
      _entitiesByIdAndType.Add(key, value);
      GetDocumentItem(key, value.GetType()).DocumentLoadedFromBackingStore(value);
   }

   public virtual TValue GetForUpdate<TValue>(object key) =>
      GetInternal<TValue>(key, useUpdateLock: true);

   public IEnumerable<TValue> GetAll<TValue>(IEnumerable<EntityId<Guid>> ids) where TValue : IEntity<Guid>
   {
      var idSet = ids.ToHashSet(); //Avoid multiple enumerations.

      var stored = _backingStore.GetAll<TValue>(idSet.Select(id => id.Value));

      stored.Where(document => !_entitiesByIdAndType.Contains(typeof(TValue), document.Id))
            .ForEach(unloadedDocument => OnInitialLoad(unloadedDocument.Id, unloadedDocument));

      var results = _entitiesByIdAndType.GetAll().Select(pair => pair.Value).OfType<TValue>().Where(candidate => idSet.Contains(candidate.Id)).ToArray();
      var missingDocuments = idSet.Where(id => results.None(result => result.Id == id)).ToArray();
      if(missingDocuments.Any())
      {
         throw new NoSuchDocumentException(missingDocuments.First(), typeof(TValue));
      }

      return results;
   }

   public virtual TValue Get<TValue>(object key)
   {
      if(TryGet(key, out TValue? value)) return value._assertNotNull();

      throw new NoSuchDocumentException(key, typeof(TValue));
   }

   TValue GetInternal<TValue>(object key, bool useUpdateLock)
   {
      if(TryGetInternal(key, typeof(TValue), out TValue? value, useUpdateLock)) return value._assertNotNull();

      throw new NoSuchDocumentException(key, typeof(TValue));
   }

   public virtual void Save<TValue>(object id, TValue value)
   {
      Argument.Fulfills(value is not null);

      if(TryGetInternal(id, value.GetType(), out TValue? _, useUpdateLock: false))
      {
         throw new AttemptToSaveAlreadyPersistedValueException(id, value);
      }

      var documentItem = GetDocumentItem(id, value.GetType());
      documentItem.Save(value);

      _entitiesByIdAndType.Add(id, value);
      documentItem.CommitChangesToBackingStore();
   }

   public virtual void Save<TEntity>(TEntity entity) where TEntity : IEntity<Guid> => Save(entity.Id, entity);

   public virtual void Delete<TEntity>(TEntity entity) where TEntity : IEntity<Guid> => Delete<TEntity>(entity.Id);

   public virtual void Delete<T>(object id)
   {
      if(!TryGet(id, out T? _))
      {
         throw new NoSuchDocumentException(id, typeof(T));
      }

      var documentItem = GetDocumentItem(id, typeof(T));
      documentItem.Delete();

      _entitiesByIdAndType.Remove(id, typeof(T));
      documentItem.CommitChangesToBackingStore();
   }

   public virtual IEnumerable<T> GetAll<T>() where T : IEntity<Guid>
   {
      var stored = _backingStore.GetAll<T>();
      stored.Where(document => !_entitiesByIdAndType.Contains(typeof(T), document.Id))
            .ForEach(unloadedDocument => OnInitialLoad(unloadedDocument.Id, unloadedDocument));
      return _entitiesByIdAndType.GetAll().Select(pair => pair.Value).OfType<T>();
   }

   public IEnumerable<Guid> GetAllIds<T>() where T : IEntity<Guid> => _backingStore.GetAllIds<T>();

   public virtual void Dispose() {}

   public override string ToString() => $"{_id}: {GetType().FullName}";

   readonly Guid _id = Guid.NewGuid();
   readonly Dictionary<Type, Dictionary<string, string>> _persistentValues = new();

   void FlushChanges() => _handledDocuments.ForEach(p => p.Value.CommitChangesToBackingStore());
}
