﻿using AccountManagement.Domain.Passwords;
using Compze.Testing;
using FluentAssertions;
using NUnit.Framework;
using static FluentAssertions.FluentActions;

namespace AccountManagement.UnitTests.Passwords;

[TestFixture]
public class A_password_is_invalid_if_it : UniversalTestBase
{
   [Test] public static void Is_null() => AssertCreatingPasswordThrowsExceptionContainingFailure(null!, Password.Policy.Failures.Null);
   [Test] public static void Is_shorter_than_four_characters() => AssertCreatingPasswordThrowsExceptionContainingFailure("abc", Password.Policy.Failures.ShorterThanFourCharacters);
   [Test] public static void Starts_with_whitespace() => AssertCreatingPasswordThrowsExceptionContainingFailure(" Pass", Password.Policy.Failures.BorderedByWhitespace);
   [Test] public static void Ends_with_whitespace() => AssertCreatingPasswordThrowsExceptionContainingFailure("Pass ", Password.Policy.Failures.BorderedByWhitespace);
   [Test] public static void Contains_only_lowercase_characters() => AssertCreatingPasswordThrowsExceptionContainingFailure("pass", Password.Policy.Failures.MissingUppercaseCharacter);
   [Test] public static void Contains_only_uppercase_characters() => AssertCreatingPasswordThrowsExceptionContainingFailure("PASS", Password.Policy.Failures.MissingLowerCaseCharacter);

   static void AssertCreatingPasswordThrowsExceptionContainingFailure(string password, Password.Policy.Failures expectedFailure)
      // ReSharper disable once ObjectCreationAsStatement
      => Invoking(() => new Password(password))
        .Should().Throw<PasswordDoesNotMatchPolicyException>().Which
        .Failures.Should().Contain(expectedFailure);
}