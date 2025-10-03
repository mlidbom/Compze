using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Compze.Tessaging.Typermedia.Implementation;

class CommandValidationFailureException(IEnumerable<ValidationResult> failures) : Exception(string.Join(Environment.NewLine, failures));