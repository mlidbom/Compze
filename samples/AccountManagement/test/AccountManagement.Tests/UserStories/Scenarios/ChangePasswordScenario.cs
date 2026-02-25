using AccountManagement.API;
using CommunityToolkit.Diagnostics;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Typermedia.Public;
using Compze.Tessaging.Hosting;

namespace AccountManagement.UserStories.Scenarios;

public class ChangePasswordScenario : ScenarioBase<AccountResource>
{
   readonly IClient _client;

   public string OldPassword { get; private set; }
   public string NewPassword { get; private set; }
   public AccountResource Account { get; private set; }

   public ChangePasswordScenario WithNewPassword(string newPassword)
   {
      NewPassword = newPassword;
      return this;
   }

   public ChangePasswordScenario WithOldPassword(string oldPassword)
   {
      OldPassword = oldPassword;
      return this;
   }

   public static ChangePasswordScenario Create(IClient client)
   {
      var registerAccountScenario = new RegisterAccountScenario(client);
      var account = registerAccountScenario.Execute().Account;

      return new ChangePasswordScenario(client, account!, registerAccountScenario.Password, TestData.Passwords.CreateValidPasswordString());
   }

   public ChangePasswordScenario(IClient client, AccountResource account, string oldPassword, string newPassword)
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