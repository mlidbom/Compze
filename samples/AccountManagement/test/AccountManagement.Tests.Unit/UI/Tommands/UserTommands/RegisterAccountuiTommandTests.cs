using AccountManagement.API;
using Compze.Tests.Infrastructure;
using Compze.Must;
using Compze.xUnitBDD;

namespace AccountManagement.Tests.Unit.UI.Tommands.UserTommands;


public class RegisterAccountUITommandTests : UniversalTestBase
{
   readonly AccountResource.Tommand.Register? _registerAccountUiTommand;

   public RegisterAccountUITommandTests()
   {
      _registerAccountUiTommand = AccountResource.Tommand.Register.Create();
      _registerAccountUiTommand.Email = "valid.email@google.com";
      _registerAccountUiTommand.Password = "AComplex!1Password";

      TommandValidator.ValidationFailures(_registerAccountUiTommand).Must().BeEmpty();
   }

   [XF]
   public void IsInvalidifAccountIdIsNull()
   {
      _registerAccountUiTommand!.AccountId = null!;
      TommandValidator.ValidationFailures(_registerAccountUiTommand).Must().NotBeEmpty();
   }

   [XF]
   public void IsInvalidIfEmailIsNull()
   {
      _registerAccountUiTommand!.Email = null!;
      TommandValidator.ValidationFailures(_registerAccountUiTommand).Must().NotBeEmpty();
   }

   [XF]
   public void IsInvalidIfEmailIsIncorrectFormat()
   {
      _registerAccountUiTommand!.Email = "invalid";
      TommandValidator.ValidationFailures(_registerAccountUiTommand).Must().NotBeEmpty();
   }

   [XF]
   public void IsInvalidIfPasswordIsNull()
   {
      _registerAccountUiTommand!.Password = null!;
      TommandValidator.ValidationFailures(_registerAccountUiTommand).Must().NotBeEmpty();
   }

   [XF]
   public void IsInvalidIfPasswordDoesNotMatchPolicy()
   {
      foreach(var invalidPassword in TestData.Passwords.Invalid.All)
      {
         _registerAccountUiTommand!.Password = invalidPassword!;
         TommandValidator.ValidationFailures(_registerAccountUiTommand).Must().NotBeEmpty();
      }
   }

   [XF]
   public void WhenNotMatchingThePolicyTheFailureTellsHow()
   {
      _registerAccountUiTommand!.Password = TestData.Passwords.Invalid.ShorterThanFourCharacters;
      ValidateAndGetFirstTessage().Must().Be(RegisterAccountTommandResources.Password_ShorterThanFourCharacters);

      _registerAccountUiTommand!.Password = TestData.Passwords.Invalid.BorderedByWhiteSpaceAtEnd;
      ValidateAndGetFirstTessage().Must().Be(RegisterAccountTommandResources.Password_BorderedByWhitespace);

      _registerAccountUiTommand.Password = TestData.Passwords.Invalid.MissingLowercaseCharacter;
      ValidateAndGetFirstTessage().Must().Be(RegisterAccountTommandResources.Password_MissingLowerCaseCharacter);

      _registerAccountUiTommand.Password = TestData.Passwords.Invalid.MissingUpperCaseCharacter;
      ValidateAndGetFirstTessage().Must().Be(RegisterAccountTommandResources.Password_MissingUpperCaseCharacter);

      _registerAccountUiTommand.Password = TestData.Passwords.Invalid.Null!;
      ValidateAndGetFirstTessage().Must().Be(RegisterAccountTommandResources.PasswordMissing);
   }

   [XF]
   public void FailsIfUnHandledPolicyFailureIsDetected()
   {
      _registerAccountUiTommand!.Password = null!; //Null is normally caught by the Require attribute.
      // ReSharper disable once AssignNullToNotNullAttribute
      // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
      _registerAccountUiTommand.Invoking(tommand => tommand.Validate(null!).ToArray()).Must().Throw<Exception>();
   }

   string ValidateAndGetFirstTessage() => TommandValidator.ValidationFailures(_registerAccountUiTommand!).First().ErrorMessage ?? string.Empty;
}