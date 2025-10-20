using System.Threading;
using Xunit;


// ReSharper disable once CheckNamespace
namespace AccountManagement;

#pragma warning disable CA1308 // Normalize strings to uppercase

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