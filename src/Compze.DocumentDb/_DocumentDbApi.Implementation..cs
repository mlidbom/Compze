using Compze.DocumentDb.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Tessaging.Typermedia.HandlerRegistration;

namespace Compze.DocumentDb;

public partial class DocumentDbApi
{
   public partial class TueryApi
   {
      public class GetDocumentForUpdate<TDocument> : TessageTypes.StrictlyLocal.Tueries.StrictlyLocalTuery<GetDocumentForUpdate<TDocument>, TDocument> where TDocument : class
      {
         internal GetDocumentForUpdate(Guid id) => Id = id;
         Guid Id { get; set; }

         internal static void RegisterHandler(TypermediaHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTuery(
            (GetDocumentForUpdate<TDocument> tuery, IDocumentDbUpdater updater) => updater.GetForUpdate<TDocument>(tuery.Id));
      }

      public class TryGetDocument<TDocument> : TessageTypes.StrictlyLocal.Tueries.StrictlyLocalTuery<TryGetDocument<TDocument>, TDocument?> where TDocument : class
      {
         internal TryGetDocument(string id) => Id = id;
         string Id { get; set; }

         internal static void RegisterHandler(TypermediaHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTuery(
            (TryGetDocument<TDocument> tuery, IDocumentDbReader updater) => updater.TryGet<TDocument>(tuery.Id, out var document) ? document : null);
      }

      public class GetReadonlyCopyOfDocument<TDocument> : TessageTypes.StrictlyLocal.Tueries.StrictlyLocalTuery<GetReadonlyCopyOfDocument<TDocument>, TDocument> where TDocument : class
      {
         internal GetReadonlyCopyOfDocument(Guid id) => Id = id;
         Guid Id { get; set; }

         internal static void RegisterHandler(TypermediaHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTuery(
            (GetReadonlyCopyOfDocument<TDocument> tuery, IDocumentDbReader reader) => reader.Get<TDocument>(tuery.Id));
      }
   }

   public partial class Tommand
   {
      public class DeleteDocument<TDocument> : TessageTypes.StrictlyLocal.Tommands.StrictlyLocalTommand where TDocument : class
      {
         internal DeleteDocument(string key) => Key = key;
         string Key { get; }

         internal static void RegisterHandler(TypermediaHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTommand(
            (DeleteDocument<TDocument> command, IDocumentDbUpdater updater) => updater.Delete<TDocument>(command.Key));
      }

      public class SaveDocument<TDocument> : TessageTypes.StrictlyLocal.Tommands.StrictlyLocalTommand where TDocument : class
      {
         internal SaveDocument(string key, TDocument entity)
         {
            Key = key;
            Entity = entity;
         }

         string Key { get; }
         TDocument Entity { get; }

         internal static void RegisterHandler(TypermediaHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTommand(
            (SaveDocument<TDocument> command, IDocumentDbUpdater updater) => updater.Save(command.Key, command.Entity));
      }
   }

   public static void HandleDocumentType<TDocument>(TypermediaHandlerRegistrarWithDependencyInjectionSupport typermediaRegistrar) where TDocument : class
   {
      TueryApi.TryGetDocument<TDocument>.RegisterHandler(typermediaRegistrar);
      TueryApi.GetReadonlyCopyOfDocument<TDocument>.RegisterHandler(typermediaRegistrar);
      TueryApi.GetDocumentForUpdate<TDocument>.RegisterHandler(typermediaRegistrar);
      Tommand.SaveDocument<TDocument>.RegisterHandler(typermediaRegistrar);
      Tommand.DeleteDocument<TDocument>.RegisterHandler(typermediaRegistrar);
   }

}