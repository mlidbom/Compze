using AccountManagement.API;
using CommunityToolkit.Diagnostics;
using Compze.Contracts;
using Compze.Functional;
using Compze.Messaging.Buses;
using Compze.Messaging.Hypermedia;

namespace AccountManagement.UserStories.Scenarios;

class ChangePasswordScenario : ScenarioBase<AccountResource>
{
   readonly IEndpoint _clientEndpoint;

   public string OldPassword;
   public string NewPassword;
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

   public static ChangePasswordScenario Create(IEndpoint domainEndpoint)
   {
      var registerAccountScenario = new RegisterAccountScenario(domainEndpoint);
      var account = registerAccountScenario.Execute().Account;

      return new ChangePasswordScenario(domainEndpoint, account, registerAccountScenario.Password, TestData.Passwords.CreateValidPasswordString());
   }

   public ChangePasswordScenario(IEndpoint clientEndpoint, AccountResource account, string oldPassword, string newPassword)
   {
      Guard.IsNotNull(account);
      _clientEndpoint = clientEndpoint;
      Account = account;
      OldPassword = oldPassword;
      NewPassword = newPassword;
   }

   public override AccountResource Execute()
   {
      Account.Commands.ChangePassword.WithValues(OldPassword, NewPassword).Post().ExecuteAsClientRequestOn(_clientEndpoint);

      return Account = Api.Query.AccountById(Account.Id).ExecuteAsClientRequestOn(_clientEndpoint);
   }
}