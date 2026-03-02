using AccountManagement.API;
using CommunityToolkit.Diagnostics;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Typermedia.Public;
using Compze.Tessaging.Hosting;

namespace AccountManagement.UserStories.Scenarios;

class ChangePasswordScenario : ScenarioBase<AccountResource>
{
   readonly IClient _client;

   internal string OldPassword { get; private set; }
   internal string NewPassword { get; private set; }
   internal AccountResource Account { get; private set; }

   internal ChangePasswordScenario WithNewPassword(string newPassword)
   {
      NewPassword = newPassword;
      return this;
   }

   internal ChangePasswordScenario WithOldPassword(string oldPassword)
   {
      OldPassword = oldPassword;
      return this;
   }

   internal static ChangePasswordScenario Create(IClient client)
   {
      var registerAccountScenario = new RegisterAccountScenario(client);
      var account = registerAccountScenario.Execute().Account;

      return new ChangePasswordScenario(client, account!, registerAccountScenario.Password, TestData.Passwords.CreateValidPasswordString());
   }

   internal ChangePasswordScenario(IClient client, AccountResource account, string oldPassword, string newPassword)
   {
      Guard.IsNotNull(account);
      _client = client;
      Account = account;
      OldPassword = oldPassword;
      NewPassword = newPassword;
   }

   public override AccountResource Execute()
   {
      Account.Tommands.ChangePassword.WithValues(OldPassword, NewPassword).Post().ExecuteRequestOn(_client);

      return Account = Api.Tuery.AccountById(Account.Id).ExecuteRequestOn(_client);
   }
}