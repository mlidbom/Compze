using System.ComponentModel.DataAnnotations;

namespace Compze.Tessaging.Validation._private;

class TommandValidationFailureException(IEnumerable<ValidationResult> failures) : Exception(string.Join(Environment.NewLine, failures));
