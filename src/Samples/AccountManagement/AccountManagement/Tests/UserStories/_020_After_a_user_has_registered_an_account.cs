using System.Threading.Tasks;
using AccountManagement.Domain.Registration;
using AccountManagement.UserStories.Scenarios;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.Infrastructure.Fluent;

namespace AccountManagement.UserStories;

public class _020_After_a_user_has_registered_an_account : UserStoryTest
{
   RegisterAccountScenario? _registerAccountScenario;

   protected override async Task InitializeAsyncInternal()
   {
      await base.InitializeAsyncInternal();
      _registerAccountScenario = Scenario.Register;
      var result = _registerAccountScenario.Execute().Result;
      result.Status.Must().Be(RegistrationAttemptStatus.Successful);
   }

   [PCT] public void Login_with_the_correct_email_and_password_succeeds()
   {
      var result = Scenario.Login(_registerAccountScenario!).Execute();

      result.Succeeded.Must().Be(true);
      result.AuthenticationToken.Must().NotBeNullOrWhiteSpace();
   }

   [PCT] public void Login_with_the_correct_email_but_wrong_password_fails()
      => Scenario.Login(_registerAccountScenario!).WithPassword("SomeOtherPassword").Execute().Succeeded.Must().Be(false);

   [PCT] public void Login_with_the_wrong_email_but_correct_password_fails()
      => Scenario.Login(_registerAccountScenario!).WithEmail("some_other@email.com").Execute().Succeeded.Must().Be(false);

   [PCT] public void Registering_another_account_with_the_same_email_fails_with_email_already_registered_tessage() =>
      Scenario.Register.WithEmail(_registerAccountScenario!.Email).Execute().Result.Status.Must().Be(RegistrationAttemptStatus.EmailAlreadyRegistered);
}
