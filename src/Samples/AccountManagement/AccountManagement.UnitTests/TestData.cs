﻿using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;

// ReSharper disable once CheckNamespace
namespace AccountManagement;

#pragma warning disable CA1308 // Normalize strings to uppercase

static class TestData
{
   internal static class Passwords
   {
      internal const string ValidPassword = "Pass";

      internal static class Invalid
      {
         public const string Null = null;
         public static readonly string EmptyString = string.Empty;
         public static readonly string ShorterThanFourCharacters = ValidPassword[..3];
         public const string BorderedByWhiteSpaceAtEnd = $"{ValidPassword} ";
         public const string BorderedByWhiteSpaceAtBeginning = $" {ValidPassword}";
         public static readonly string MissingUpperCaseCharacter = ValidPassword.ToLowerInvariant();
         public static readonly string MissingLowercaseCharacter = ValidPassword.ToUpperInvariant();

         public static readonly string[] All =
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

      internal static IReadOnlyList<StringTestData> InvalidEmailsTestData =>
         new List<StringTestData>
         {
            new(null, "Is null"),
            new(string.Empty, "Is empty"),
            new("test.com", "Missing @ character"),
            new("test@test.com ", "Missing domain"),
            new("te st@test.com", "Contains space"),
            new("test@test", "Missing domain"),
            new("test@test..com", "Contains \"..\""),
            new("test@.test.com", "Contains \"@.\""),
            new("test.@test.com", "Contains \".@\"")
         };


      public class StringTestData(string data, string description) : TestData<string>(data, description);

      public class TestData<TData> : TestCaseData
         where TData : class
      {
         public TData Data { get; }
         public TestData(TData data, string description) : base(data)
         {
            Data = data;
            SetName($"_({description} ==  \"{data?.ToString() ?? "NULL"}\")");
         }
      }
   }
}