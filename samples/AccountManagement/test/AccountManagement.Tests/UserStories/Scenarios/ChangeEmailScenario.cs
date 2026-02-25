using AccountManagement.API;
using AccountManagement.Domain;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Typermedia.Public;
using Compze.Tessaging.Hosting;

namespace AccountManagement.UserStories.Scenarios;

public class ChangeAccountEmailScenario : ScenarioBase<AccountResource>
{
   readonly IClient _client;

   public string NewEmail { get; private set;} = TestData.Emails.CreateUnusedEmail();
   public Email OldEmail { get; }

   public ChangeAccountEmailScenario WithNewEmail(string newEmail)
   {
      NewEmail = newEmail;
      return this;
   }

   public AccountResource Account { get; private set; }

   public static ChangeAccountEmailScenario Create(IClient client) => new(client, new RegisterAccountScenario(client).Execute().Account!);

   public ChangeAccountEmailScenario(IClient client, AccountResource account)
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