using Xunit;

// ReSharper disable ClassNeverInstantiated.Global

namespace Compze.Utilities.Testing.XUnit.v3.Tests.NCrunchErrorReproductions;

static class TestData
{
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

public class TheoriesCrashInNcrunchChurnMode
{
   public class An_InvalidEmailException_containing_the_email_is_thrown_if_email
   {
      [Theory, MemberData(nameof(TestData.Emails.InvalidEmailsTestData), MemberType = typeof(TestData.Emails))]
      public void _(string? invalidEmail, string _) {}
   }
}
