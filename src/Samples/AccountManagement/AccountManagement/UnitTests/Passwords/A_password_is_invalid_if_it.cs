using AccountManagement.Domain.Passwords;
using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.Fluent;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Utilities.Testing.Fluent.MustActions;


namespace AccountManagement.Tests.Unit.Passwords;


public class A_password_is_invalid_if_it : UniversalTestBase
{
   [XF] public static void Is_null() => AssertCreatingPasswordThrowsExceptionContainingFailure(null!, Password.Policy.Failures.Null);
   [XF] public static void Is_shorter_than_four_characters() => AssertCreatingPasswordThrowsExceptionContainingFailure("abc", Password.Policy.Failures.ShorterThanFourCharacters);
   [XF] public static void Starts_with_whitespace() => AssertCreatingPasswordThrowsExceptionContainingFailure(" Pass", Password.Policy.Failures.BorderedByWhitespace);
   [XF] public static void Ends_with_whitespace() => AssertCreatingPasswordThrowsExceptionContainingFailure("Pass ", Password.Policy.Failures.BorderedByWhitespace);
   [XF] public static void Contains_only_lowercase_characters() => AssertCreatingPasswordThrowsExceptionContainingFailure("pass", Password.Policy.Failures.MissingUppercaseCharacter);
   [XF] public static void Contains_only_uppercase_characters() => AssertCreatingPasswordThrowsExceptionContainingFailure("PASS", Password.Policy.Failures.MissingLowerCaseCharacter);

   static void AssertCreatingPasswordThrowsExceptionContainingFailure(string password, Password.Policy.Failures expectedFailure)
      // ReSharper disable once ObjectCreationAsStatement
      => Invoking(() => new Password(password))
        .Must().Throw<PasswordDoesNotMatchPolicyException>().Which
        .Failures.Must().Contain(expectedFailure);
}