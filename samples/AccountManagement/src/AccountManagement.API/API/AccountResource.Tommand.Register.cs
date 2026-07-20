using AccountManagement.API.ValidationAttributes;
using AccountManagement.Domain;
using Compze.Abstractions.Public;
using JetBrains.Annotations;
using System.ComponentModel.DataAnnotations;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Abstractions.TessageTypes;

// ReSharper disable MemberCanBeMadeStatic.Global Because we want these members to be accessed through the fluent API we don't want to make them static.

namespace AccountManagement.API;

public partial class AccountResource
{
   public static partial class Tommand
   {
      public partial class Register : Remotable.AtMostOnce.AtMostOnceTypermediaTommand<Register.RegistrationAttemptResult>, IValidatableObject
      {
         public static Register Create() => new()
                                            {
                                               AccountId = new AccountId(),
                                               Id = new TessageId()
                                            };

         //Note the use of a custom validation attributes.
         [TaggregateId(ErrorMessageResourceType = typeof(RegisterAccountTommandResources), ErrorMessageResourceName = "IdInvalid")]
         [Required(ErrorMessageResourceType = typeof(RegisterAccountTommandResources), ErrorMessageResourceName = "IdMissing")]
         public AccountId AccountId { [UsedImplicitly] get; set; } = new();

         [Email(ErrorMessageResourceType = typeof(RegisterAccountTommandResources), ErrorMessageResourceName = "EmailInvalid")]
         [Required(ErrorMessageResourceType = typeof(RegisterAccountTommandResources), ErrorMessageResourceName = "EmailMissing")]
         public string Email { [UsedImplicitly] get; set; } = string.Empty;

         [Required(ErrorMessageResourceType = typeof(RegisterAccountTommandResources), ErrorMessageResourceName = "PasswordMissing")]
         public string Password { get; set; } = string.Empty;

         public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Domain.Passwords.Password.Validate(Password, this, () => Password);

         internal Register WithValues(AccountId accountId, string email, string password) => new()
                                                                                             {
                                                                                                Id = new TessageId(),
                                                                                                AccountId = accountId,
                                                                                                Email = email,
                                                                                                Password = password
                                                                                             };
      }
   }
}