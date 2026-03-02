using AccountManagement.API;
using AccountManagement.Domain;
using Compze.Core.Tessaging.Hosting.Public;

namespace AccountManagement.UserStories.Scenarios;

public class AccountScenarioApi(IClient client)
{
   readonly IClient _client = client;

   public RegisterAccountScenario Register => new(_client);

   internal ChangeAccountEmailScenario ChangeEmail() => ChangeAccountEmailScenario.Create(_client);
   internal ChangeAccountEmailScenario ChangeEmail(AccountResource account) => new(_client, account);

   public ChangePasswordScenario ChangePassword() => ChangePasswordScenario.Create(_client);
   internal ChangePasswordScenario ChangePassword(AccountResource account, string oldPassword, string newPassword) => new(_client, account, oldPassword: oldPassword, newPassword: newPassword);

   internal LoginScenario Login() => LoginScenario.Create(_client);
   internal LoginScenario Login(RegisterAccountScenario registrationScenario) => new(_client, registrationScenario.Email, registrationScenario.Password);
   public LoginScenario Login(Email email, string password) => new(_client, email: email.StringValue, password: password);
   public LoginScenario Login(string email, string password) => new(_client, email: email, password: password);
}