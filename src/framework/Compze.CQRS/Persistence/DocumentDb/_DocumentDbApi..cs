using System;
using Composable.DDD;
// ReSharper disable MemberCanBeMadeStatic.Global we want composable fluent APIs. No statics please.

namespace Composable.Persistence.DocumentDb;

public partial class DocumentDbApi
{
   public QueryApi Queries => new();
   public Command Commands => new();

   public partial class QueryApi
   {
      public TryGetDocument<TDocument> TryGet<TDocument>(Guid id) where TDocument : IHasPersistentIdentity<Guid> => new(id.ToString());

      public TryGetDocument<TDocument> TryGet<TDocument>(string id) => new(id);

      public GetDocumentForUpdate<TDocument> GetForUpdate<TDocument>(Guid id) => new(id);

      public GetReadonlyCopyOfDocument<TDocument> GetReadOnlyCopy<TDocument>(Guid id) => new(id);
   }

   public partial class Command
   {
      public SaveDocument<TDocument> Save<TDocument>(string key, TDocument account) => new(key, account);

      public SaveDocument<TDocument> Save<TDocument>(TDocument account) where TDocument : IHasPersistentIdentity<Guid> => new(account.Id.ToString(), account);

      public DeleteDocument<TDocument> Delete<TDocument>(string key) => new(key);
   }
}