using System;
using Compze.Core.DocumentDb.Public;
using Compze.Core.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Core.Tessaging.Public;
using Compze.Utilities.Functional;

namespace Compze.Core.DocumentDb;

public partial class DocumentDbApi
{
   public partial class TueryApi
   {
      public class GetDocumentForUpdate<TDocument> : TessageTypes.StrictlyLocal.Tueries.StrictlyLocalTuery<GetDocumentForUpdate<TDocument>, TDocument>
      {
         public GetDocumentForUpdate(Guid id) => Id = id;
         Guid Id { get; set; }

         public static void RegisterHandler(TessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTuery(
            (GetDocumentForUpdate<TDocument> tuery, IDocumentDbUpdater updater) => updater.GetForUpdate<TDocument>(tuery.Id));
      }

      public class TryGetDocument<TDocument> : TessageTypes.StrictlyLocal.Tueries.StrictlyLocalTuery<TryGetDocument<TDocument>, Option<TDocument>>
      {
         public TryGetDocument(string id) => Id = id;
         string Id { get; set; }

         public static void RegisterHandler(TessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTuery(
            (TryGetDocument<TDocument> tuery, IDocumentDbReader updater) => updater.TryGet<TDocument>(tuery.Id, out var document) ? Option.Some(document) : Option.None<TDocument>());
      }

      public class GetReadonlyCopyOfDocument<TDocument> : TessageTypes.StrictlyLocal.Tueries.StrictlyLocalTuery<GetReadonlyCopyOfDocument<TDocument>, TDocument>
      {
         public GetReadonlyCopyOfDocument(Guid id) => Id = id;
         Guid Id { get; set; }

         public static void RegisterHandler(TessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTuery(
            (GetReadonlyCopyOfDocument<TDocument> tuery, IDocumentDbReader reader) => reader.Get<TDocument>(tuery.Id));
      }
   }

   public partial class Tommand
   {
      public class DeleteDocument<TDocument> : TessageTypes.StrictlyLocal.Tommands.StrictlyLocalTommand
      {
         public DeleteDocument(string key) => Key = key;
         string Key { get; }

         public static void RegisterHandler(TessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTommand(
            (DeleteDocument<TDocument> command, IDocumentDbUpdater updater) => updater.Delete<TDocument>(command.Key));
      }

      public class SaveDocument<TDocument> : TessageTypes.StrictlyLocal.Tommands.StrictlyLocalTommand
      {
         public SaveDocument(string key, TDocument entity)
         {
            Key = key;
            Entity = entity;
         }

         string Key { get; }
         TDocument Entity { get; }

         public static void RegisterHandler(TessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForTommand(
            (DocumentDbApi.Tommand.SaveDocument<TDocument> command, IDocumentDbUpdater updater) => updater.Save(command.Key, command.Entity));
      }
   }

   public static void HandleDocumentType<TDocument>(TessageHandlerRegistrarWithDependencyInjectionSupport registrar)
   {
      TueryApi.TryGetDocument<TDocument>.RegisterHandler(registrar);
      TueryApi.GetReadonlyCopyOfDocument<TDocument>.RegisterHandler(registrar);
      TueryApi.GetDocumentForUpdate<TDocument>.RegisterHandler(registrar);
      Tommand.SaveDocument<TDocument>.RegisterHandler(registrar);
      Tommand.DeleteDocument<TDocument>.RegisterHandler(registrar);
   }

}