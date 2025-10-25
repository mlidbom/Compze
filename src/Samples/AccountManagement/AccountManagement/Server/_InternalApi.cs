using System;
using AccountManagement.Domain;
using AccountManagement.UI.QueryModels;
using CommunityToolkit.Diagnostics;
using Compze.Abstractions.Tessaging.Public;
using Compze.Tessaging.TyperMediaApi.TeventStore;
using Compze.Utilities.Functional;

// ReSharper disable MemberCanBeMadeStatic.Global we want _composable_ fluent APIs which does not happen with static members since we need instances to compose the API.

namespace AccountManagement;

static class InternalApi
{
   static TeventStoreApi TeventStore => new TeventStoreApi();
   internal static Tuery Queries => new();
   internal static Tommand Tommands => new();
   internal static AccountQueryModel.Api AccountQueryModel => new();

   internal class Tuery
   {
      internal TryGetByEmailTuery TryGetByEmail(Email email) => new(email);

      internal TeventStoreApi.TueryApi.AggregateLink<Account> GetForUpdate(Guid id) => TeventStore.Queries.GetForUpdate<Account>(id);

      internal TeventStoreApi.TueryApi.GetReadonlyCopyOfAggregate<Account> GetReadOnlyCopy(Guid id) => TeventStore.Queries.GetReadOnlyCopy<Account>(id);

      internal TeventStoreApi.TueryApi.GetReadonlyCopyOfAggregateVersion<Account> GetReadOnlyCopyOfVersion(Guid id, int version) => TeventStore.Queries.GetReadOnlyCopyOfVersion<Account>(id, version);

      internal class TryGetByEmailTuery : IStrictlyLocalTuery<TryGetByEmailTuery, Option<Account>>
      {
         public TryGetByEmailTuery(Email email)
         {
            Guard.IsNotNull(email);
            Email = email;
         }

         internal Email Email { get; private set; }
      }
   }

   internal class Tommand
   {
      internal TeventStoreApi.TommandApi.SaveAggregate<Account> Save(Account account) => TeventStore.Tommands.Save(account);
   }
}