using AccountManagement.API;
using AccountManagement.Domain;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Typermedia.Public;
using Compze.Tessaging.Hosting;

namespace AccountManagement.UserStories.Scenarios;

public class ChangeAccountEmailScenario : ScenarioBase<AccountResource>
{
   readonly IEndpoint _clientEndpoint;

   public string NewEmail { get; private set;} = TestData.Emails.CreateUnusedEmail();
   public Email OldEmail { get; }

   public ChangeAccountEmailScenario WithNewEmail(string newEmail)
   {
      NewEmail = newEmail;
      return this;
   }

   public AccountResource Account { get; private set; }

   public static ChangeAccountEmailScenario Create(IEndpoint domainEndpoint) => new(domainEndpoint, new RegisterAccountScenario(domainEndpoint).Execute().Account!);

   public ChangeAccountEmailScenario(IEndpoint clientEndpoint, AccountResource account)
   {
      _clientEndpoint = clientEndpoint;
      Account = account;
      OldEmail = Account.Email;
   }

   public override AccountResource Execute()
   {
      Account.Tommands.ChangeEmail.WithEmail(NewEmail).Post().ExecuteAsClientRequestOn(_clientEndpoint);

      return Account = Api.Tuery.AccountById(Account.Id).ExecuteAsClientRequestOn(_clientEndpoint);
   }
}