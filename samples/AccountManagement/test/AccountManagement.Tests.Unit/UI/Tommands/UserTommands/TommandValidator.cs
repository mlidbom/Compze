using System.ComponentModel.DataAnnotations;

namespace AccountManagement.Tests.Unit.UI.Tommands.UserTommands;

static class TommandValidator
{
   public static IEnumerable<ValidationResult> ValidationFailures(object tommand)
   {
      var context = new ValidationContext(tommand, serviceProvider: null, items: null);
      var results = new List<ValidationResult>();

      Validator.TryValidateObject(tommand, context, results, validateAllProperties: true);
      return results;
   }
}