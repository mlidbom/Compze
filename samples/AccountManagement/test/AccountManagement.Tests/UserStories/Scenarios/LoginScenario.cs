using AccountManagement.API;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Tessaging.Hosting;

namespace AccountManagement.UserStories.Scenarios;

public class LoginScenario(IClient client, string email, string password) : ScenarioBase<AccountResource.Tommand.LogIn.LoginAttemptResult>
{
   readonly IClient _client = client;
   public string Password { get; set; } = password;
   public string Email { get; set; } = email;

   internal LoginScenario WithEmail(string email)
   {
      Email = email;
      return this;
   }

   internal LoginScenario WithPassword(string password)
   {
      Password = password;
      return this;
   }

   internal static LoginScenario Create(IClient client)
   {
      var registerAccountScenario = new RegisterAccountScenario(client);
      registerAccountScenario.Execute();
      return new LoginScenario(client, registerAccountScenario.Email, registerAccountScenario.Password);
   }

   public LoginScenario(IClient client, AccountResource account, string password) : this(client, account.Email.ToString(), password) {}

   public override AccountResource.Tommand.LogIn.LoginAttemptResult Execute() => Api.Tommand.Login(Email, Password).ExecuteRequestOn(_client);
}