using System;
using AccountManagement.Domain;
using AccountManagement.UI.QueryModels;
using Compze;
using Compze.Contracts;
using Compze.Functional;
using Compze.Messaging;
using Compze.Persistence.EventStore;

// ReSharper disable MemberCanBeMadeStatic.Global we want _composable_ fluent APIs which does not happen with static members since we need instances to compose the API.

namespace AccountManagement;

static class InternalApi
{
   static CompzeApi CompzeApi => new();
   internal static Query Queries => new();
   internal static Command Commands => new();
   internal static AccountQueryModel.Api AccountQueryModel => new();

   internal class Query
   {
      internal TryGetByEmailQuery TryGetByEmail(Email email) => new(email);

      internal EventStoreApi.QueryApi.AggregateLink<Account> GetForUpdate(Guid id) => CompzeApi.EventStore.Queries.GetForUpdate<Account>(id);

      internal EventStoreApi.QueryApi.GetReadonlyCopyOfAggregate<Account> GetReadOnlyCopy(Guid id) => CompzeApi.EventStore.Queries.GetReadOnlyCopy<Account>(id);

      internal EventStoreApi.QueryApi.GetReadonlyCopyOfAggregateVersion<Account> GetReadOnlyCopyOfVersion(Guid id, int version) => CompzeApi.EventStore.Queries.GetReadOnlyCopyOfVersion<Account>(id, version);

      internal class TryGetByEmailQuery : IStrictlyLocalQuery<TryGetByEmailQuery, Option<Account>>
      {
         public TryGetByEmailQuery(Email accountId)
         {
            Contract.ArgumentNotNullOrDefault(accountId, nameof(Account));
            Email = accountId;
         }

         internal Email Email { get; private set; }
      }
   }

   internal class Command
   {
      internal EventStoreApi.Command.SaveAggregate<Account> Save(Account account) => CompzeApi.EventStore.Commands.Save(account);
   }
}