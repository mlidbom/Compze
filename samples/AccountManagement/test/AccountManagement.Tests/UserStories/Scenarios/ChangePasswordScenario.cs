using AccountManagement.API;
using CommunityToolkit.Diagnostics;
using Compze.Core.Tessaging.Typermedia.Public;
using Compze.Tessaging.Abstractions.Tessaging.Typermedia.Public;

namespace AccountManagement.UserStories.Scenarios;

class ChangePasswordScenario : ScenarioBase<AccountResource>
{
   readonly IRemoteTypermediaNavigator _navigator;

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

   internal static ChangePasswordScenario Create(IRemoteTypermediaNavigator navigator)
   {
      var registerAccountScenario = new RegisterAccountScenario(navigator);
      var account = registerAccountScenario.Execute().Account;

      return new ChangePasswordScenario(navigator, account!, registerAccountScenario.Password, TestData.Passwords.CreateValidPasswordString());
   }

   ChangePasswordScenario(IRemoteTypermediaNavigator navigator, AccountResource account, string oldPassword, string newPassword)
   {
      Guard.IsNotNull(account);
      _navigator = navigator;
      Account = account;
      OldPassword = oldPassword;
      NewPassword = newPassword;
   }

   public override AccountResource Execute()
   {
      Account.Tommands.ChangePassword.WithValues(OldPassword, NewPassword).Post().NavigateOn(_navigator);

      return Account = _navigator.Navigate(Api.Tuery.AccountById(Account.Id));
   }
}