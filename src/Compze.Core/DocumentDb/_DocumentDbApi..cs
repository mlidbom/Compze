using System;
using Compze.Core.Public;

// ReSharper disable MemberCanBeMadeStatic.Global we want composable fluent APIs. No statics please.

namespace Compze.Core.DocumentDb;

public partial class DocumentDbApi
{
   public TueryApi Tueries => new();
   public Tommand Tommands => new();

   public partial class TueryApi
   {
      public TryGetDocument<TDocument> TryGet<TDocument>(Guid id) where TDocument : IEntity<Guid> => new(id.ToString());

      public TryGetDocument<TDocument> TryGet<TDocument>(string id) => new(id);

      public GetDocumentForUpdate<TDocument> GetForUpdate<TDocument>(Guid id) => new(id);

      public GetReadonlyCopyOfDocument<TDocument> GetReadOnlyCopy<TDocument>(Guid id) => new(id);
   }

   public partial class Tommand
   {
      public SaveDocument<TDocument> Save<TDocument>(string key, TDocument account) => new(key, account);

      public SaveDocument<TDocument> Save<TDocument>(TDocument account) where TDocument : IEntity<Guid> => new(account.Id.ToString(), account);

      public DeleteDocument<TDocument> Delete<TDocument>(string key) => new(key);
   }
}