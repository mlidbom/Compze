using Xunit;


// ReSharper disable once CheckNamespace
namespace AccountManagement;

#pragma warning disable CA1308 // Normalize strings to uppercase

static class TestData
{
   internal static class Passwords
   {
      const string ValidPassword = "Pass";

      internal static class Invalid
      {
         public const string? Null = null;
         static readonly string EmptyString = string.Empty;
         public static readonly string ShorterThanFourCharacters = ValidPassword[..3];
         public const string BorderedByWhiteSpaceAtEnd = $"{ValidPassword} ";
         const string BorderedByWhiteSpaceAtBeginning = $" {ValidPassword}";
         public static readonly string MissingUpperCaseCharacter = ValidPassword.ToLowerInvariant();
         public static readonly string MissingLowercaseCharacter = ValidPassword.ToUpperInvariant();

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
      public static TheoryData<string?, string> InvalidEmailsTestData =>
         new()
         {
            { null, "Is null" },
            { string.Empty, "Is empty" },
            { "test.com", "Missing @ character" },
            { "test@test.com ", "Missing domain" },
            { "te st@test.com", "Contains space" },
            { "test@test", "Missing domain" },
            { "test@test..com", "Contains \"..\"" },
            { "test@.test.com", "Contains \"@.\"" },
            { "test.@test.com", "Contains \".@\"" }
         };
   }
}
