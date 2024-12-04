using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Compze.Messaging.Buses.Implementation;

class CommandValidationFailureException(IEnumerable<ValidationResult> failures) : Exception(CreateMessage(failures))
{
   public IEnumerable<ValidationResult> Failures { get; } = failures;

   static string CreateMessage(IEnumerable<ValidationResult> failures) => string.Join(Environment.NewLine, failures);
}