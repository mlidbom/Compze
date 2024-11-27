using AccountManagement.API.ValidationAttributes.Helpers;
using AccountManagement.Domain;

namespace AccountManagement.API.ValidationAttributes;

public sealed class EmailAttribute : StringValidationAttribute
{
   protected override bool IsValid(string value) => string.IsNullOrEmpty(value) || Email.IsValidEmail(value);
}