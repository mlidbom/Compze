using AccountManagement.Domain;
using AccountManagement.Domain.Passwords;
using Newtonsoft.Json;

namespace AccountManagement.API;

public partial class AccountResource
{
#pragma warning disable IDE0051 // Remove unused private members
   [JsonConstructor]AccountResource(AccountId id, Email email, Password password, AccountTommands tommands)
#pragma warning restore IDE0051 // Remove unused private members
   {
      Id = id;
      Email = email;
      Password = password;
      Tommands = tommands;
   }

   public AccountId Id { get; private set; }

   //Todo:review: this conversion smells
   public AccountResource(IAccountResourceData account)
   {
      Id = account.Id;
      Tommands = new AccountTommands(this);
      Email = account.Email;
      Password = account.Password;
   }

   public Email Email { get; private set; }
   public Password Password { get; private set; }

   public AccountTommands Tommands { get; private set; }
}
