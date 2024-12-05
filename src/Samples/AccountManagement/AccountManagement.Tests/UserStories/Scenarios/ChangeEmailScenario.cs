using AccountManagement.API;
using AccountManagement.Domain;
using Compze.Messaging.Buses;
using Compze.Messaging.Hypermedia;

namespace AccountManagement.UserStories.Scenarios;

class ChangeAccountEmailScenario : ScenarioBase<AccountResource>
{
   readonly IEndpoint _clientEndpoint;

   public string NewEmail = TestData.Emails.CreateUnusedEmail();
   public readonly Email OldEmail;

   public ChangeAccountEmailScenario WithNewEmail(string newEmail)
   {
      NewEmail = newEmail;
      return this;
   }

   public AccountResource Account { get; private set; }

   public static ChangeAccountEmailScenario Create(IEndpoint domainEndpoint) => new(domainEndpoint, new RegisterAccountScenario(domainEndpoint).Execute().Account);

   public ChangeAccountEmailScenario(IEndpoint clientEndpoint, AccountResource account)
   {
      _clientEndpoint = clientEndpoint;
      Account = account;
      OldEmail = Account.Email;
   }

   public override AccountResource Execute()
   {
      Account.Commands.ChangeEmail.WithEmail(NewEmail).Post().ExecuteAsClientRequestOn(_clientEndpoint);

      return Account = Api.Query.AccountById(Account.Id).ExecuteAsClientRequestOn(_clientEndpoint);
   }
}