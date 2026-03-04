using AccountManagement.Domain.Passwords;
using Compze.Tests.Infrastructure;
using Compze.Must;
using Compze.xUnit.BDD;
using static Compze.Must.MustActions;


namespace AccountManagement.Tests.Unit.Passwords;


public class A_password_is_invalid_if_it : UniversalTestBase
{
   [XF] public void Is_null() => AssertCreatingPasswordThrowsExceptionContainingFailure(null!, Password.Policy.Failures.Null);
   [XF] public void Is_shorter_than_four_characters() => AssertCreatingPasswordThrowsExceptionContainingFailure("abc", Password.Policy.Failures.ShorterThanFourCharacters);
   [XF] public void Starts_with_whitespace() => AssertCreatingPasswordThrowsExceptionContainingFailure(" Pass", Password.Policy.Failures.BorderedByWhitespace);
   [XF] public void Ends_with_whitespace() => AssertCreatingPasswordThrowsExceptionContainingFailure("Pass ", Password.Policy.Failures.BorderedByWhitespace);
   [XF] public void Contains_only_lowercase_characters() => AssertCreatingPasswordThrowsExceptionContainingFailure("pass", Password.Policy.Failures.MissingUppercaseCharacter);
   [XF] public void Contains_only_uppercase_characters() => AssertCreatingPasswordThrowsExceptionContainingFailure("PASS", Password.Policy.Failures.MissingLowerCaseCharacter);

   static void AssertCreatingPasswordThrowsExceptionContainingFailure(string password, Password.Policy.Failures expectedFailure)
      // ReSharper disable once ObjectCreationAsStatement
      => Invoking(() => new Password(password))
        .Must().Throw<PasswordDoesNotMatchPolicyException>().Which
        .Failures.Must().Contain(expectedFailure);
}