using Compze.Internals.SystemCE.LinqCE;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Must;
using Compze.Must.Assertions;

namespace AccountManagement.UserStories;

public class _030_When_a_user_attempt_to_change_their_password_the_operation_fails_if : UserStoryTest
{
   [PCT] public void New_password_is_invalid() =>
      TestData.Passwords.Invalid.All.ForEach(invalidPassword => Scenario.ChangePassword().WithNewPassword(invalidPassword!).ExecutingShouldThrow<Exception>());

   [PCT] public void OldPassword_is_null() => Scenario.ChangePassword().WithOldPassword(null!).ExecutingShouldThrow<Exception>();

   [PCT] public void OldPassword_is_empty_string() => Scenario.ChangePassword().WithOldPassword("").ExecutingShouldThrow<Exception>();

   [PCT] public void OldPassword_is_not_the_current_password_of_the_account() =>
      Scenario.ChangePassword().WithOldPassword("Wrong").ExecutingShouldThrow<Exception>().Which.Message.ToUpperInvariant().Must().Contain("PASSWORD").Contain("WRONG");
}