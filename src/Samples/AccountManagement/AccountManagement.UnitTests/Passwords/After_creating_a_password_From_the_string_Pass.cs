using AccountManagement.Domain.Passwords;
using FluentAssertions;
using NUnit.Framework;

// ReSharper disable InconsistentNaming

namespace AccountManagement.UnitTests.Passwords;

static class After_creating_a_password_From_the_string_Pass
{
   static readonly Password _password = new("Pass");

   [Test] public static void HashedPassword_is_not_null() => _password.GetHash().Should().NotBeNull();
   [Test] public static void HashedPassword_is_not_an_empty_array() => _password.GetHash().Should().NotBeEmpty();
   [Test] public static void Salt_is_not_null() => _password.GetSalt().Should().NotBeNull();

   [Test] public static void IsCorrectPassword_returns_true_if_string_is_Pass() => _password.IsCorrectPassword("Pass").Should().BeTrue();

   [Test] public static void IsCorrectPassword_returns_false_if__case_changes()
   {
      _password.IsCorrectPassword("pass").Should().BeFalse();
      _password.IsCorrectPassword("PasS").Should().BeFalse();
   }

   [Test] public static void IsCorrectPassword_returns__if_space_is_prepended() => _password.IsCorrectPassword(" Pass").Should().BeFalse();
   [Test] public static void IsCorrectPassword_returns__if_space_is_appended() => _password.IsCorrectPassword("Pass ").Should().BeFalse();
}