using System;
using AccountManagement.Domain;
using AccountManagement.UI.QueryModels;
using CommunityToolkit.Diagnostics;
using Compze.Abstractions.Tessaging.Public;
using Compze.Tessaging.TyperMediaApi.EventStore;
using Compze.Utilities.Functional;

// ReSharper disable MemberCanBeMadeStatic.Global we want _composable_ fluent APIs which does not happen with static members since we need instances to compose the API.

namespace AccountManagement;

static class InternalApi
{
   static EventStoreApi EventStore => new EventStoreApi();
   internal static Query Queries => new();
   internal static Command Commands => new();
   internal static AccountQueryModel.Api AccountQueryModel => new();

   internal class Query
   {
      internal TryGetByEmailQuery TryGetByEmail(Email email) => new(email);

      internal EventStoreApi.QueryApi.AggregateLink<Account> GetForUpdate(Guid id) => EventStore.Queries.GetForUpdate<Account>(id);

      internal EventStoreApi.QueryApi.GetReadonlyCopyOfAggregate<Account> GetReadOnlyCopy(Guid id) => EventStore.Queries.GetReadOnlyCopy<Account>(id);

      internal EventStoreApi.QueryApi.GetReadonlyCopyOfAggregateVersion<Account> GetReadOnlyCopyOfVersion(Guid id, int version) => EventStore.Queries.GetReadOnlyCopyOfVersion<Account>(id, version);

      internal class TryGetByEmailQuery : IStrictlyLocalQuery<TryGetByEmailQuery, Option<Account>>
      {
         public TryGetByEmailQuery(Email email)
         {
            Guard.IsNotNull(email);
            Email = email;
         }

         internal Email Email { get; private set; }
      }
   }

   internal class Command
   {
      internal EventStoreApi.CommandApi.SaveAggregate<Account> Save(Account account) => EventStore.Commands.Save(account);
   }
}