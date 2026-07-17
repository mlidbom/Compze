using AccountManagement.API;
using AccountManagement.Domain;
using Compze.Tessaging.Typermedia;

namespace AccountManagement.UserStories.Scenarios;

public class AccountScenarioApi(IRemoteTypermediaNavigator navigator)
{
   readonly IRemoteTypermediaNavigator _navigator = navigator;

   public RegisterAccountScenario Register => new(_navigator);

   internal ChangeAccountEmailScenario ChangeEmail() => ChangeAccountEmailScenario.Create(_navigator);
   internal ChangeAccountEmailScenario ChangeEmail(AccountResource account) => new(_navigator, account);

   internal ChangePasswordScenario ChangePassword() => ChangePasswordScenario.Create(_navigator);

   internal LoginScenario Login(RegisterAccountScenario registrationScenario) => new(_navigator, registrationScenario.Email, registrationScenario.Password);
   internal LoginScenario Login(Email email, string password) => new(_navigator, email: email.StringValue, password: password);
   public LoginScenario Login(string email, string password) => new(_navigator, email: email, password: password);
}