﻿using System;
using Compze.SystemCE.LinqCE;
using JetBrains.Annotations;
using NUnit.Framework;

namespace AccountManagement.UserStories;

public class _010_When_a_user_registers_an_account_the_operation_fails_if([NotNull] string pluggableComponentsCombination) : UserStoryTest(pluggableComponentsCombination)
{
   [Test] public void Password_is_invalid() =>
      TestData.Passwords.Invalid.All.ForEach(invalidPassword => Scenario.Register
                                                                        .WithPassword(invalidPassword)
                                                                        .ExecutingShouldThrow<Exception>());

   [Test] public void Email_is_invalid() =>
      TestData.Emails.InvalidEmails.ForEach(invalidEmail => Scenario.Register
                                                                    .WithEmail(invalidEmail)
                                                                    .ExecutingShouldThrow<Exception>());

   [Test] public void AccountId_is_empty()
      => Scenario.Register.WithAccountId(Guid.Empty).ExecutingShouldThrow<Exception>();
}