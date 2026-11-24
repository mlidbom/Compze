// ReSharper disable All

#region usings
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.BestEffort;
using Compze.Tessaging.TessageTypes;
using Compze.TypeIdentifiers.DependencyInjection;
#endregion

#pragma warning disable // Documentation example code: deliberately illustrative, not production code.

namespace Compze.Tessaging.Typermedia.Documentation.DeclaringAnApi;

#region start
//The API's front door. Getting it takes no round trip: the caller creates the start resource itself.
public static class AccountsApi
{
   public static Remotable.NonTransactional.Tueries.NewableResultLink<AccountsStart> Start => new();
}

public class AccountsStart
{
   //A link: the tuery that gets an account.
   public AccountLink Account(Guid accountId) => new(accountId);

   //A form: the tommand that opens an account.
   public OpenAccount OpenAccount(string email) => new(email);
}
#endregion

#region resource
public class AccountLink(Guid accountId) : Remotable.NonTransactional.Tueries.Tuery<AccountResource>
{
   public Guid AccountId { get; private set; } = accountId;
}

public class AccountResource(Guid accountId, string email)
{
   public Guid AccountId { get; private set; } = accountId;
   public string Email { get; private set; } = email;

   //A form the account hands out already filled in with its own id: the caller supplies only the new email.
   public ChangeEmail ChangeEmail(string newEmail) => new(AccountId, newEmail);
}
#endregion

#region tommands
public class OpenAccount(string email) : Remotable.AtMostOnce.AtMostOnceTypermediaTommand<AccountResource>
{
   public string Email { get; private set; } = email;
}

public class ChangeEmail(Guid accountId, string newEmail) : Remotable.AtMostOnce.AtMostOnceTypermediaTommand<AccountResource>
{
   public Guid AccountId { get; private set; } = accountId;
   public string NewEmail { get; private set; } = newEmail;
}
#endregion

#region serving_endpoint
class AccountsApiEndpoint : BestEffortEndpointDeclaration<AccountsApiEndpoint>, IEndpointIdentity
{
   public static string Name => "AccountsApi";
   public static EndpointId Id { get; } = new(Guid.Parse("2F6B9E1D-7C43-4A85-B0D2-93E1C8A54F07"));

   protected override void RegisterComponents(IComponentRegistrar registrar) => registrar
      .RequireMappedTypesFromAssemblyContaining<AccountLink>()
      .Register(Singleton.For<Accounts>().CreatedBy(() => new Accounts()));

   protected override void RegisterTueryHandlers(ITueryHandlerRegistrar handle) => handle
      .ForTuery((AccountLink link, Accounts accounts) => accounts.Get(link.AccountId));

   protected override void RegisterTypermediaTommandHandlers(ITypermediaTommandHandlerRegistrar handle) => handle
      .ForTommand((OpenAccount tommand, Accounts accounts) => accounts.Open(tommand.Email))
      .ForTommand((ChangeEmail tommand, Accounts accounts) => accounts.ChangeEmail(tommand.AccountId, tommand.NewEmail));
}
#endregion

#region accounts
class Accounts
{
   readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, AccountResource> _accounts = new();

   public AccountResource Get(Guid accountId) => _accounts[accountId];

   public AccountResource Open(string email)
   {
      var account = new AccountResource(Guid.NewGuid(), email);
      _accounts[account.AccountId] = account;
      return account;
   }

   public AccountResource ChangeEmail(Guid accountId, string newEmail) => _accounts[accountId] = new AccountResource(accountId, newEmail);
}
#endregion
