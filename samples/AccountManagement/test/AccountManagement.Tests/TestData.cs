using System.Collections.Generic;
using System.Threading;


#pragma warning disable CA1308 // Normalize strings to uppercase

namespace AccountManagement;

static class TestData
{
   internal static class Passwords
   {
      internal const string ValidPassword = "Pass";

      internal static class Invalid
      {
         const string? Null = null;
         static readonly string EmptyString = string.Empty;
         static readonly string ShorterThanFourCharacters = ValidPassword[..3];
         const string BorderedByWhiteSpaceAtEnd = $"{ValidPassword} ";
         const string BorderedByWhiteSpaceAtBeginning = $" {ValidPassword}";
         static readonly string MissingUpperCaseCharacter = ValidPassword.ToLowerInvariant();
         static readonly string MissingLowercaseCharacter = ValidPassword.ToUpperInvariant();

         public static readonly string?[] All =
         [
            Null,
            EmptyString,
            ShorterThanFourCharacters,
            BorderedByWhiteSpaceAtBeginning,
            BorderedByWhiteSpaceAtEnd,
            MissingUpperCaseCharacter,
            MissingLowercaseCharacter
         ];
      }

      static int _passwordCount = 1;
      internal static string CreateValidPasswordString() => $"{ValidPassword}{_passwordCount++}";
   }

   internal static class Emails
   {
      static int _registeredAccounts = 1;

      internal static string CreateUnusedEmail() => $"test.test@test{Interlocked.Increment(ref _registeredAccounts)}.se";

      internal static IEnumerable<string?> InvalidEmails =>
         new List<string?>
         {
            null,
            string.Empty,
            "test.com",
            "test@test.com ",
            "te st@test.com",
            "test@test",
            "test@test..com",
            "test@.test.com",
            "test.@test.com"
         };
   }
}
