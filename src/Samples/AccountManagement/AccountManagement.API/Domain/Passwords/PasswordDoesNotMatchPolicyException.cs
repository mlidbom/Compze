﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace AccountManagement.Domain.Passwords;

public class PasswordDoesNotMatchPolicyException : ArgumentException
{
    internal PasswordDoesNotMatchPolicyException(IEnumerable<Password.Policy.Failures> passwordPolicyFailures) : base(BuildMessage(passwordPolicyFailures)) => Failures = passwordPolicyFailures;

    public IEnumerable<Password.Policy.Failures> Failures { get; private set; }

    static string BuildMessage(IEnumerable<Password.Policy.Failures> passwordPolicyFailures)
    {
        return string.Join(",", passwordPolicyFailures.Select(failure => failure.ToString()));
    }
}