using Compze.Abstractions.Tessaging.Public;
using JetBrains.Annotations;

namespace AccountManagement.API;

[UsedImplicitly] public class StartResource
{
   public Tommand Tommands { get; private set; } = new();

   public Tuery Queries { get; private set; } = new();

#pragma warning disable CA1724 // Type names should not match namespaces
   public class Tuery
#pragma warning restore CA1724 // Type names should not match namespaces
   {
      public TessageTypes.Remotable.NonTransactional.Queries.EntityLink<AccountResource> AccountById { get; private set; } = new();
   }

   public class Tommand
   {
      public AccountResource.Tommand.LogIn Login { get; private set; } = AccountResource.Tommand.LogIn.Create();
      public AccountResource.Tommand.Register Register { get; private set; } = AccountResource.Tommand.Register.Create();
   }
}