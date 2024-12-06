using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Compze.Messaging.Buses.Implementation;

class CommandValidationFailureException(IEnumerable<ValidationResult> failures) : Exception(string.Join(Environment.NewLine, failures));