using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Compze.Tessaging.Common.Typermedia.Implementation;

class CommandValidationFailureException(IEnumerable<ValidationResult> failures) : Exception(string.Join(Environment.NewLine, failures));