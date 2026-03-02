using AccountManagement.API;
using AccountManagement.Domain;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Typermedia.Public;
using Compze.Tessaging.Hosting;

namespace AccountManagement.UserStories.Scenarios;

class ChangeAccountEmailScenario : ScenarioBase<AccountResource>
{
   readonly IClient _client;

   internal string NewEmail { get; private set;} = TestData.Emails.CreateUnusedEmail();
   internal Email OldEmail { get; }

   internal ChangeAccountEmailScenario WithNewEmail(string newEmail)
   {
      NewEmail = newEmail;
      return this;
   }

   internal AccountResource Account { get; private set; }

   internal static ChangeAccountEmailScenario Create(IClient client) => new(client, new RegisterAccountScenario(client).Execute().Account!);

   internal ChangeAccountEmailScenario(IClient client, AccountResource account)
   {
      _client = client;
      Account = account;
      OldEmail = Account.Email;
   }

   public override AccountResource Execute()
   {
      Account.Tommands.ChangeEmail.WithEmail(NewEmail).Post().ExecuteRequestOn(_client);

      return Account = Api.Tuery.AccountById(Account.Id).ExecuteRequestOn(_client);
   }
}