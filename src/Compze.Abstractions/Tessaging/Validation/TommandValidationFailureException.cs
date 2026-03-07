using System.ComponentModel.DataAnnotations;

namespace Compze.Abstractions.Tessaging.Validation;

public class TommandValidationFailureException(IEnumerable<ValidationResult> failures) : Exception(string.Join(Environment.NewLine, failures));
