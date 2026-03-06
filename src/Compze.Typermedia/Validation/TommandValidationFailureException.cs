using System.ComponentModel.DataAnnotations;

namespace Compze.Typermedia.Validation;

public class TommandValidationFailureException(IEnumerable<ValidationResult> failures) : Exception(string.Join(Environment.NewLine, failures));
