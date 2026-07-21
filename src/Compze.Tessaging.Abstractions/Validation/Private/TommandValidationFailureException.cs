using System.ComponentModel.DataAnnotations;

namespace Compze.Tessaging.Validation.Private;

class TommandValidationFailureException(IEnumerable<ValidationResult> failures) : Exception(string.Join(Environment.NewLine, failures));
