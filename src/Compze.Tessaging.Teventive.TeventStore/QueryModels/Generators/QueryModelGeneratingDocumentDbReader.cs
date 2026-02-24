using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Compze.Core.DocumentDb.Infrastructure;
using Compze.Core.Public;
using Compze.Core.Tessaging.Teventive.TeventStore.QueryModels.Generators.Public;
using Compze.Utilities.SystemCE.UsageGuards;
using Compze.Contracts;
using static Compze.Contracts.Assert;

namespace Compze.Tessaging.Teventive.TeventStore.QueryModels.Generators;

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

   public virtual TValue Get<TValue>(EntityId key)
   {
      _usageGuard.EnsureAccessValid();
      if(TryGet(key, out TValue? value))
      {
         return ReturnValue.ReturnNotNull(value);
      }

      throw new NoSuchDocumentException(key, typeof(TValue));
   }

   public virtual TValue GetVersion<TValue>(EntityId key, int version)
   {
      _usageGuard.EnsureAccessValid();
      if(TryGetVersion(key, out TValue? value, version))
      {
         return ReturnValue.ReturnNotNull(value);
      }

      throw new NoSuchDocumentException(key, typeof(TValue));
   }

   public virtual bool TryGet<TDocument>(EntityId key, [MaybeNullWhen(false)] out TDocument document) => TryGetVersion(key, out document);

   public virtual bool TryGetVersion<TDocument>(EntityId key, [MaybeNullWhen(false)] out TDocument document, int version = -1)
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
         throw new ArgumentException("You cannot tuery by id for an interface type. There is no guarantee of uniqueness");
      }

      if(!requiresVersioning && _entitiesByIdAndType.TryGet(key, out document) && documentType.IsInstanceOfType(document))
      {
         return true;
      }

      var result = TryGenerateModel<TDocument>(key, version);
      if(result is not null)
      {
         document = result;
         if(!requiresVersioning)
         {
            _entitiesByIdAndType.Add(key, document);
         }

         return true;
      }

      return false;
   }

   TDocument? TryGenerateModel<TDocument>(EntityId key, int version)
   {
      if(version < 0)
      {
         return GetGeneratorsForDocumentType<TDocument>()
               .Select(generator => generator.TryGenerate(key))
               .Single();
      }

      return VersionedGeneratorsForDocumentType<TDocument>()
            .Select(generator => generator.TryGenerate(key, version))
            .Single();
   }

   bool HandlesDocumentType<TDocument>(bool requireVersioningSupport) => requireVersioningSupport
                                                                            ? VersionedGeneratorsForDocumentType<TDocument>().Any()
                                                                            : GetGeneratorsForDocumentType<TDocument>().Any();

   public virtual IEnumerable<TValue> GetAll<TValue>(IEnumerable<EntityId> ids) where TValue : IEntity
   {
      _usageGuard.EnsureAccessValid();
      return ids.Select(id => Get<TValue>(id)).ToList();
   }

   IEnumerable<IVersioningQueryModelGenerator<TDocument>> VersionedGeneratorsForDocumentType<TDocument>() => _documentGenerators.OfType<IVersioningQueryModelGenerator<TDocument>>().ToList();

   IEnumerable<IQueryModelGenerator<TDocument>> GetGeneratorsForDocumentType<TDocument>() => _documentGenerators.OfType<IQueryModelGenerator<TDocument>>().ToList();
}
