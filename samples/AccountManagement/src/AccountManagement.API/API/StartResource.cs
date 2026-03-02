using AccountManagement.Domain;
using Compze.Core.Tessaging.Public;
using JetBrains.Annotations;

namespace AccountManagement.API;

[UsedImplicitly] public class StartResource
{
   internal TommandsResource TommandsResources { get; private set; } = new();

   internal TueriesResource Tueries { get; private set; } = new();

#pragma warning disable CA1724 // Type names should not match namespaces
   public class TueriesResource
#pragma warning restore CA1724 // Type names should not match namespaces
   {
      internal TessageTypes.Remotable.NonTransactional.Tueries.TaggregateLink<AccountResource> AccountById(AccountId accountId) => new(accountId);
   }

   public class TommandsResource
   {
      internal AccountResource.Tommand.LogIn Login { get; private set; } = AccountResource.Tommand.LogIn.Create();
      internal AccountResource.Tommand.Register Register { get; private set; } = AccountResource.Tommand.Register.Create();
   }
}