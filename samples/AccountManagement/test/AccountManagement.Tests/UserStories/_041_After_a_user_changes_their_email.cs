using AccountManagement.Domain.Registration;
using AccountManagement.UserStories.Scenarios;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Must;

namespace AccountManagement.UserStories;

public class _041_After_a_user_changes_their_email : UserStoryTest
{
   ChangeAccountEmailScenario? _changeEmailScenario;
   RegisterAccountScenario? _registerAccountScenario;

   protected override async Task InitializeAsyncInternal()
   {
      await base.InitializeAsyncInternal();
      _registerAccountScenario = Scenario.Register;
      var (result, account) = _registerAccountScenario.Execute();
      if(result.Status != RegistrationAttemptStatus.Successful) throw new Exception($"Registration failed. Status: {result.Status.ToString()}");
      _changeEmailScenario = Scenario.ChangeEmail(account!);
      _changeEmailScenario.Execute();
   }

   [PCT] public void Logging_in_with_the_old_email__does_not_work() => Scenario.Login(_registerAccountScenario!).Execute().Succeeded.Must().Be(false);

   [PCT] public void Logging_in_with_the_new_email_works() => Scenario.Login(_registerAccountScenario!).WithEmail(_changeEmailScenario!.NewEmail).Execute().Succeeded.Must().Be(true);

   [PCT] public void Account_Email_is_the_new_email() => _changeEmailScenario!.Account.Email.StringValue.Must().Be(_changeEmailScenario.NewEmail);

   [PCT] public void Registering_an_account_with_the_old_email_works() => Scenario.Register.WithEmail(_changeEmailScenario!.OldEmail.ToString()).Execute();

   [PCT] public void Attempting_to_register_an_account_with_the_new_email_fails_with_email_already_registered_tessage() =>
      Scenario.Register.WithEmail(_changeEmailScenario!.NewEmail).Execute()
              .Result.Status
              .Must().Be(RegistrationAttemptStatus.EmailAlreadyRegistered);
}