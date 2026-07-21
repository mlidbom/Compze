using System.Diagnostics.CodeAnalysis;
using Compze.Abstractions;
using Compze.Contracts;
using Compze.DocumentDb.Infrastructure;
using Compze.Teventive.TeventStore.Abstractions.QueryModels.Generators;

namespace Compze.Teventive.TeventStore.QueryModels.Generators;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class QueryModelGeneratingQueryModelReader : IVersioningQueryModelReader
{
   readonly IEnumerable<IQueryModelGenerator> _documentGenerators;
   readonly EntitiesByIdAndTypeCache _entitiesByIdAndType = new();

   public QueryModelGeneratingQueryModelReader(IEnumerable<IQueryModelGenerator> documentGenerators)
   {
      _documentGenerators = documentGenerators;
   }

   public virtual TValue Get<TValue>(EntityId key) where TValue : class
   {
      if(TryGet(key, out TValue? value))
      {
         return value._assert().NotNull();
      }

      throw new NoSuchQueryModelException(key, typeof(TValue));
   }

   public virtual TValue GetVersion<TValue>(EntityId key, int version) where TValue : class
   {
      if(TryGetVersion(key, out TValue? value, version))
      {
         return value._assert().NotNull();
      }

      throw new NoSuchQueryModelException(key, typeof(TValue));
   }

   protected virtual bool TryGet<TDocument>(EntityId key, [NotNullWhen(true)] out TDocument? document) where TDocument : class => TryGetVersion(key, out document);

   protected virtual bool TryGetVersion<TDocument>(EntityId key, [NotNullWhen(true)] out TDocument? document, int version = -1) where TDocument : class
   {
      var requiresVersioning = version > 0;

      document = null;

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

   IEnumerable<IVersioningQueryModelGenerator<TDocument>> VersionedGeneratorsForDocumentType<TDocument>() => _documentGenerators.OfType<IVersioningQueryModelGenerator<TDocument>>().ToList();

   IEnumerable<IQueryModelGenerator<TDocument>> GetGeneratorsForDocumentType<TDocument>() => _documentGenerators.OfType<IQueryModelGenerator<TDocument>>().ToList();
}
