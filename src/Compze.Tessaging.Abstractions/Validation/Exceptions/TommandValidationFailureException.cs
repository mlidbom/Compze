using System.ComponentModel.DataAnnotations;

namespace Compze.Tessaging.Validation.Exceptions;

class TommandValidationFailureException(IEnumerable<ValidationResult> failures) : Exception(string.Join(Environment.NewLine, failures));
