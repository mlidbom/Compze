using AccountManagement.API;
using AccountManagement.Domain;
using Compze.Core.Tessaging.Hosting.Public;

namespace AccountManagement.UserStories.Scenarios;

public class AccountScenarioApi
{
   readonly IClient _client;
   public AccountScenarioApi(IClient client) => _client = client;

   public RegisterAccountScenario Register => new(_client);

   public ChangeAccountEmailScenario ChangeEmail() => ChangeAccountEmailScenario.Create(_client);
   public ChangeAccountEmailScenario ChangeEmail(AccountResource account) => new(_client, account);

   public ChangePasswordScenario ChangePassword() => ChangePasswordScenario.Create(_client);
   public ChangePasswordScenario ChangePassword(AccountResource account, string oldPassword, string newPassword) => new(_client, account, oldPassword: oldPassword, newPassword: newPassword);

   public LoginScenario Login() => LoginScenario.Create(_client);
   public LoginScenario Login(RegisterAccountScenario registrationScenario) => new(_client, registrationScenario.Email, registrationScenario.Password);
   public LoginScenario Login(Email email, string password) => new(_client, email: email.StringValue, password: password);
   public LoginScenario Login(string email, string password) => new(_client, email: email, password: password);
}