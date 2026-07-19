using System.ComponentModel.DataAnnotations;

namespace Compze.Tessaging.Abstractions.Validation;

public class TommandValidationFailureException(IEnumerable<ValidationResult> failures) : Exception(string.Join(Environment.NewLine, failures));
