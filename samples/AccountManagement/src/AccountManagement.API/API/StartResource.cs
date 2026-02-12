using AccountManagement.Domain;
using Compze.Core.Tessaging.Public;
using JetBrains.Annotations;

namespace AccountManagement.API;

[UsedImplicitly] public class StartResource
{
   public TommandsResource TommandsResources { get; private set; } = new();

   public TueriesResource Tueries { get; private set; } = new();

#pragma warning disable CA1724 // Type names should not match namespaces
   public class TueriesResource
#pragma warning restore CA1724 // Type names should not match namespaces
   {
      public TessageTypes.Remotable.NonTransactional.Tueries.TaggregateLink<AccountResource> AccountById(AccountId accountId) => new(accountId);
   }

   public class TommandsResource
   {
      public AccountResource.Tommand.LogIn Login { get; private set; } = AccountResource.Tommand.LogIn.Create();
      public AccountResource.Tommand.Register Register { get; private set; } = AccountResource.Tommand.Register.Create();
   }
}