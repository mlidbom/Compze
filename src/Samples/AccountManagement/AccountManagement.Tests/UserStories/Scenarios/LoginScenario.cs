using AccountManagement.API;
using Composable.Functional;
using Composable.Messaging.Buses;

namespace AccountManagement.UserStories.Scenarios;

class LoginScenario(IEndpoint clientEndpoint, string email, string password) : ScenarioBase<AccountResource.Command.LogIn.LoginAttemptResult>
{
   readonly IEndpoint _clientEndpoint = clientEndpoint;
   public string Password { get; set; } = password;
   public string Email { get; set; } = email;

   public LoginScenario WithEmail(string email) => this.mutate(it => it.Email = email);
   public LoginScenario WithPassword(string password) => this.mutate(it => it.Password = password);

   public static LoginScenario Create(IEndpoint clientEndpoint)
   {
      var registerAccountScenario = new RegisterAccountScenario(clientEndpoint);
      registerAccountScenario.Execute();
      return new LoginScenario(clientEndpoint, registerAccountScenario.Email, registerAccountScenario.Password);
   }

   public LoginScenario(IEndpoint clientEndpoint, AccountResource account, string password) : this(clientEndpoint, account.Email.ToString(), password) {}

   public override AccountResource.Command.LogIn.LoginAttemptResult Execute() => Api.Command.Login(Email, Password).ExecuteAsClientRequestOn(_clientEndpoint);
}