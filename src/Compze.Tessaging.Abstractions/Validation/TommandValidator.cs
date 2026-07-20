using System.ComponentModel.DataAnnotations;
using Compze.Tessaging.Abstractions.TessageTypes;
using Compze.Tessaging.Abstractions.Validation.Exceptions;

namespace Compze.Tessaging.Abstractions.Validation;

static class TommandValidator
{
   public static void AssertTommandIsValid(ITommand tommand)
   {
      var failures = ValidationFailures(tommand);
      if(failures.Any())
      {
         throw new TommandValidationFailureException(failures);
      }
   }

   static IReadOnlyList<ValidationResult> ValidationFailures(object tommand)
   {
      var context = new ValidationContext(tommand, serviceProvider: null, items: null);
      var results = new List<ValidationResult>();

      Validator.TryValidateObject(tommand, context, results, validateAllProperties: true);
      return results;
   }
}
