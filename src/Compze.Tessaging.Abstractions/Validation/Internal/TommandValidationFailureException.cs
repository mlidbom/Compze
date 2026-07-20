using System.ComponentModel.DataAnnotations;

namespace Compze.Tessaging.Validation.Internal;

class TommandValidationFailureException(IEnumerable<ValidationResult> failures) : Exception(string.Join(Environment.NewLine, failures));
