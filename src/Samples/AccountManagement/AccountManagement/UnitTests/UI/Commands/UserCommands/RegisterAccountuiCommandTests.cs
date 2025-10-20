using System;
using System.Linq;
using AccountManagement.API;
using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.XUnit.BDD;
using FluentAssertions;



namespace AccountManagement.Tests.Unit.UI.Commands.UserCommands;


public class RegisterAccountUICommandTests : UniversalTestBase
{
   readonly AccountResource.Command.Register? _registerAccountUiCommand;

   public RegisterAccountUICommandTests()
   {
      _registerAccountUiCommand = AccountResource.Command.Register.Create();
      _registerAccountUiCommand.Email = "valid.email@google.com";
      _registerAccountUiCommand.Password = "AComplex!1Password";

      CommandValidator.ValidationFailures(_registerAccountUiCommand).Should().BeEmpty();
   }

   [XF]
   public void IsInvalidifAccountIdIsEmpty()
   {
      _registerAccountUiCommand!.AccountId = Guid.Empty;
      CommandValidator.ValidationFailures(_registerAccountUiCommand).Should().NotBeEmpty();
   }

   [XF]
   public void IsInvalidIfEmailIsNull()
   {
      _registerAccountUiCommand!.Email = null!;
      CommandValidator.ValidationFailures(_registerAccountUiCommand).Should().NotBeEmpty();
   }

   [XF]
   public void IsInvalidIfEmailIsIncorrectFormat()
   {
      _registerAccountUiCommand!.Email = "invalid";
      CommandValidator.ValidationFailures(_registerAccountUiCommand).Should().NotBeEmpty();
   }

   [XF]
   public void IsInvalidIfPasswordIsNull()
   {
      _registerAccountUiCommand!.Password = null!;
      CommandValidator.ValidationFailures(_registerAccountUiCommand).Should().NotBeEmpty();
   }

   [XF]
   public void IsInvalidIfPasswordDoesNotMatchPolicy()
   {
      foreach(var invalidPassword in TestData.Passwords.Invalid.All)
      {
         _registerAccountUiCommand!.Password = invalidPassword!;
         CommandValidator.ValidationFailures(_registerAccountUiCommand).Should().NotBeEmpty();
      }
   }

   [XF]
   public void WhenNotMatchingThePolicyTheFailureTellsHow()
   {
      _registerAccountUiCommand!.Password = TestData.Passwords.Invalid.ShorterThanFourCharacters;
      ValidateAndGetFirstMessage().Should().Be(RegisterAccountCommandResources.Password_ShorterThanFourCharacters);

      _registerAccountUiCommand!.Password = TestData.Passwords.Invalid.BorderedByWhiteSpaceAtEnd;
      ValidateAndGetFirstMessage().Should().Be(RegisterAccountCommandResources.Password_BorderedByWhitespace);

      _registerAccountUiCommand.Password = TestData.Passwords.Invalid.MissingLowercaseCharacter;
      ValidateAndGetFirstMessage().Should().Be(RegisterAccountCommandResources.Password_MissingLowerCaseCharacter);

      _registerAccountUiCommand.Password = TestData.Passwords.Invalid.MissingUpperCaseCharacter;
      ValidateAndGetFirstMessage().Should().Be(RegisterAccountCommandResources.Password_MissingUpperCaseCharacter);

      _registerAccountUiCommand.Password = TestData.Passwords.Invalid.Null!;
      ValidateAndGetFirstMessage().Should().Be(RegisterAccountCommandResources.PasswordMissing);
   }

   [XF]
   public void FailsIfUnHandledPolicyFailureIsDetected()
   {
      _registerAccountUiCommand!.Password = null!; //Null is normally caught by the Require attribute.
      // ReSharper disable once AssignNullToNotNullAttribute
      // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
      _registerAccountUiCommand.Invoking(command => command.Validate(null!).ToArray()).Should().Throw<Exception>();
   }

   string ValidateAndGetFirstMessage() => CommandValidator.ValidationFailures(_registerAccountUiCommand!).First().ErrorMessage ?? string.Empty;
}