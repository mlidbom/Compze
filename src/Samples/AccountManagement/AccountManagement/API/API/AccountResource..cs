using AccountManagement.Domain;
using AccountManagement.Domain.Passwords;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.BaseClasses.Public;
using Newtonsoft.Json;

namespace AccountManagement.API;

public partial class AccountResource : TaggregateViewModel<AccountResource>
{
#pragma warning disable IDE0051 // Remove unused private members
   [JsonConstructor]AccountResource(Email email, Password password, AccountTommands tommands)
#pragma warning restore IDE0051 // Remove unused private members
   {
      Email = email;
      Password = password;
      Tommands = tommands;
   }

   public override AccountId Id => new(base.Id.Value);

   //Todo: this conversion smells
   internal AccountResource(IAccountResourceData account) : base(account.Id)
   {
      Tommands = new AccountTommands(this);
      Email = account.Email;
      Password = account.Password;
   }

   public Email Email { get; private set; }
   public Password Password { get; private set; }

   public AccountTommands Tommands { get; private set; }
}