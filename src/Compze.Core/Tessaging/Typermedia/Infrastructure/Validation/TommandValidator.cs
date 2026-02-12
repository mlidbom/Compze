using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Typermedia.Public;

namespace Compze.Core.Tessaging.Typermedia.Infrastructure.Validation;

public static class TommandValidator
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