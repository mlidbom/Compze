using AccountManagement.Domain.Passwords;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit.TestFrameworkExtensions;
using FluentAssertions;



// ReSharper disable InconsistentNaming

namespace AccountManagement.Tests.Unit.Passwords;


public class After_creating_a_password_From_the_string_Pass : UniversalTestBase
{
   static readonly Password _password = new("Pass");

   [XF] public static void HashedPassword_is_not_null() => _password.GetHash().Should().NotBeNull();
   [XF] public static void HashedPassword_is_not_an_empty_array() => _password.GetHash().Should().NotBeEmpty();
   [XF] public static void Salt_is_not_null() => _password.GetSalt().Should().NotBeNull();

   [XF] public static void IsCorrectPassword_returns_true_if_string_is_Pass() => _password.IsCorrectPassword("Pass").Should().BeTrue();

   [XF] public static void IsCorrectPassword_returns_false_if__case_changes()
   {
      _password.IsCorrectPassword("pass").Should().BeFalse();
      _password.IsCorrectPassword("PasS").Should().BeFalse();
   }

   [XF] public static void IsCorrectPassword_returns__if_space_is_prepended() => _password.IsCorrectPassword(" Pass").Should().BeFalse();
   [XF] public static void IsCorrectPassword_returns__if_space_is_appended() => _password.IsCorrectPassword("Pass ").Should().BeFalse();
}