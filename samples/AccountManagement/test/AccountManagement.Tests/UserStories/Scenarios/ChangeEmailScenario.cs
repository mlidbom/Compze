using AccountManagement.API;
using AccountManagement.Domain;
using Compze.Core.Tessaging.Typermedia.Public;
using Compze.Tessaging.Abstractions.Tessaging.Typermedia.Public;

namespace AccountManagement.UserStories.Scenarios;

class ChangeAccountEmailScenario : ScenarioBase<AccountResource>
{
   readonly IRemoteTypermediaNavigator _navigator;

   internal string NewEmail { get; private set;} = TestData.Emails.CreateUnusedEmail();
   internal Email OldEmail { get; }

   internal ChangeAccountEmailScenario WithNewEmail(string newEmail)
   {
      NewEmail = newEmail;
      return this;
   }

   internal AccountResource Account { get; private set; }

   internal static ChangeAccountEmailScenario Create(IRemoteTypermediaNavigator navigator) => new(navigator, new RegisterAccountScenario(navigator).Execute().Account!);

   internal ChangeAccountEmailScenario(IRemoteTypermediaNavigator navigator, AccountResource account)
   {
      _navigator = navigator;
      Account = account;
      OldEmail = Account.Email;
   }

   public override AccountResource Execute()
   {
      Account.Tommands.ChangeEmail.WithEmail(NewEmail).Post().NavigateOn(_navigator);

      return Account = _navigator.Navigate(Api.Tuery.AccountById(Account.Id));
   }
}