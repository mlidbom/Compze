using System.ComponentModel.DataAnnotations;

namespace Compze.Tessaging.Abstractions.Validation.Exceptions;

class TommandValidationFailureException(IEnumerable<ValidationResult> failures) : Exception(string.Join(Environment.NewLine, failures));
