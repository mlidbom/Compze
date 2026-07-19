using AccountManagement.Domain;
using AccountManagement.UI.QueryModels;
using CommunityToolkit.Diagnostics;
using Compze.Abstractions.Public;
using Compze.Tessaging.Abstractions.TessageTypes;
using Compze.Teventive.TeventStore.Typermedia;

// ReSharper disable MemberCanBeMadeStatic.Global we want _composable_ fluent APIs which does not happen with static members since we need instances to compose the API.

namespace AccountManagement;

static class InternalApi
{
   static TeventStoreApi TeventStore => new();
   internal static Tuery Tueries => new();
   internal static Tommand Tommands => new();
   internal static AccountQueryModel.Api AccountQueryModel => new();

   internal class Tuery
   {
      internal TryGetByEmailTuery TryGetByEmail(Email email) => new(email);

      internal TeventStoreApi.TueryApi.TaggregateLink<Account> GetForUpdate(TaggregateId id) => TeventStore.Tueries.GetForUpdate<Account>(id);

      internal TeventStoreApi.TueryApi.GetReadonlyCopyOfTaggregateVersion<Account> GetReadOnlyCopyOfVersion(TaggregateId id, int version) => TeventStore.Tueries.GetReadOnlyCopyOfVersion<Account>(id, version);

      internal class TryGetByEmailTuery : IStrictlyLocalTuery<TryGetByEmailTuery, Account?>
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
      internal TeventStoreApi.TommandApi.SaveTaggregate<Account> Save(Account account) => TeventStore.Tommands.Save(account);
   }
}
