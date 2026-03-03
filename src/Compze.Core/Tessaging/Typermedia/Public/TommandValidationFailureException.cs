using System.ComponentModel.DataAnnotations;

namespace Compze.Core.Tessaging.Typermedia.Public;

class TommandValidationFailureException(IEnumerable<ValidationResult> failures) : Exception(string.Join(Environment.NewLine, failures));