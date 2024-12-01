using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Compze.Messaging.Buses.Implementation;

public class CommandValidationFailureException : Exception
{
   public IEnumerable<ValidationResult> Failures { get; }

   public CommandValidationFailureException(IEnumerable<ValidationResult> failures) : base(CreateMessage(failures)) => Failures = failures;

   static string CreateMessage(IEnumerable<ValidationResult> failures) => string.Join(Environment.NewLine, failures);
}