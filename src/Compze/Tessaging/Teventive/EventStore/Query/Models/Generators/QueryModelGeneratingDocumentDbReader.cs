using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Compze.Abstractions;
using Compze.Abstractions.Internal.Persistence.DocumentDb;
using Compze.Persistence.Common;
using Compze.Utilities.Functional;
using Compze.Utilities.Threading;
using static Compze.Utilities.Contracts.Assert;

namespace Compze.Tessaging.Teventive.EventStore.Query.Models.Generators;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class QueryModelGeneratingQueryModelReader : IVersioningQueryModelReader
{
   readonly IUsageGuard _usageGuard;
   readonly IEnumerable<IQueryModelGenerator> _documentGenerators;
   readonly EntitiesByIdAndTypeCache _entitiesByIdAndType = new();

   public QueryModelGeneratingQueryModelReader(IEnumerable<IQueryModelGenerator> documentGenerators)
   {
      _documentGenerators = documentGenerators;
      _usageGuard = new SingleThreadUseGuard(this);
   }

   public virtual TValue Get<TValue>(object key)
   {
      _usageGuard.EnsureAccessValid();
      if(TryGet(key, out TValue? value))
      {
         return Result.ReturnNotNull(value);
      }

      throw new NoSuchDocumentException(key, typeof(TValue));
   }

   public virtual TValue GetVersion<TValue>(object key, int version)
   {
      _usageGuard.EnsureAccessValid();
      if(TryGetVersion(key, out TValue? value, version))
      {
         return Result.ReturnNotNull(value);
      }

      throw new NoSuchDocumentException(key, typeof(TValue));
   }

   public virtual bool TryGet<TDocument>(object key, [MaybeNullWhen(false)] out TDocument document) => TryGetVersion(key, out document);

   public virtual bool TryGetVersion<TDocument>(object key, [MaybeNullWhen(false)] out TDocument document, int version = -1)
   {
      var requiresVersioning = version > 0;
      _usageGuard.EnsureAccessValid();

      document = default;

      if(!HandlesDocumentType<TDocument>(requireVersioningSupport: requiresVersioning))
      {
         return false;
      }

      var documentType = typeof(TDocument);

      if(documentType.IsInterface)
      {
         throw new ArgumentException("You cannot query by id for an interface type. There is no guarantee of uniqueness");
      }

      if(!requiresVersioning && _entitiesByIdAndType.TryGet(key, out document) && documentType.IsInstanceOfType(document))
      {
         return true;
      }

      var option = TryGenerateModel<TDocument>(key, version);
      if(option is Some<TDocument> returned)
      {
         document = returned.Value;
         if(!requiresVersioning)
         {
            _entitiesByIdAndType.Add(key, document);
         }

         return true;
      }

      return false;
   }

   Option<TDocument> TryGenerateModel<TDocument>(object key, int version)
   {
      if(version < 0)
      {
         return GetGeneratorsForDocumentType<TDocument>()
               .Select(generator => generator.TryGenerate((Guid)key))
               .Single();
      }

      return VersionedGeneratorsForDocumentType<TDocument>()
            .Select(generator => generator.TryGenerate((Guid)key, version))
            .Single();
   }

   bool HandlesDocumentType<TDocument>(bool requireVersioningSupport) => requireVersioningSupport
                                                                            ? VersionedGeneratorsForDocumentType<TDocument>().Any()
                                                                            : GetGeneratorsForDocumentType<TDocument>().Any();

   public virtual IEnumerable<TValue> GetAll<TValue>(IEnumerable<Guid> ids) where TValue : IHasPersistentIdentity<Guid>
   {
      _usageGuard.EnsureAccessValid();
      return ids.Select(id => Get<TValue>(id)).ToList();
   }

   IEnumerable<IVersioningQueryModelGenerator<TDocument>> VersionedGeneratorsForDocumentType<TDocument>() => _documentGenerators.OfType<IVersioningQueryModelGenerator<TDocument>>().ToList();

   IEnumerable<IQueryModelGenerator<TDocument>> GetGeneratorsForDocumentType<TDocument>() => _documentGenerators.OfType<IQueryModelGenerator<TDocument>>().ToList();

   protected virtual void Dispose(bool disposing) {}
}
