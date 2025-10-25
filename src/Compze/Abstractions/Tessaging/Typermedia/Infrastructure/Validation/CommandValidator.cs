using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Typermedia.Public;

namespace Compze.Abstractions.Tessaging.Typermedia.Infrastructure.Validation;

static class CommandValidator
{
   public static void AssertCommandIsValid(ITommand tommand)
   {
      var failures = ValidationFailures(tommand);
      if(failures.Any())
      {
         throw new CommandValidationFailureException(failures);
      }
   }

   static IReadOnlyList<ValidationResult> ValidationFailures(object command)
   {
      var context = new ValidationContext(command, serviceProvider: null, items: null);
      var results = new List<ValidationResult>();

      Validator.TryValidateObject(command, context, results, validateAllProperties: true);
      return results;
   }
}