using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AccountManagement.API.ValidationAttributes;
using Compze.Core.Tessaging.Public;
using JetBrains.Annotations;
// ReSharper disable MemberCanBeMadeStatic.Global Because we want these members to be accessed through the fluent API we don't want to make them static.

namespace AccountManagement.API;

public partial class AccountResource
{
   public static partial class Tommand
   {
      public partial class Register() : TessageTypes.Remotable.AtMostOnce.AtMostOnceTypermediaTommand<Register.RegistrationAttemptResult>(DeduplicationIdHandling.Reuse), IValidatableObject
      {
         public static Register Create() => new()
                                            {
                                               AccountId = Guid.NewGuid(),
                                               Id = Guid.CreateVersion7()
                                            };

         //Note the use of a custom validation attributes.
         [EntityId(ErrorMessageResourceType = typeof(RegisterAccountTommandResources), ErrorMessageResourceName = "IdInvalid")]
         [Required(ErrorMessageResourceType = typeof(RegisterAccountTommandResources), ErrorMessageResourceName = "IdMissing")]
         public Guid AccountId { [UsedImplicitly] get; set; } = Guid.NewGuid();

         [Email(ErrorMessageResourceType = typeof(RegisterAccountTommandResources), ErrorMessageResourceName = "EmailInvalid")]
         [Required(ErrorMessageResourceType = typeof(RegisterAccountTommandResources), ErrorMessageResourceName = "EmailMissing")]
         public string Email { [UsedImplicitly] get; set; } = string.Empty;

         [Required(ErrorMessageResourceType = typeof(RegisterAccountTommandResources), ErrorMessageResourceName = "PasswordMissing")]
         public string Password { get; set; } = string.Empty;

         public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Domain.Passwords.Password.Validate(Password, this, () => Password);

         internal Register WithValues(Guid accountId, string email, string password) => new()
                                                                                        {
                                                                                           Id = Guid.CreateVersion7(),
                                                                                           AccountId = accountId,
                                                                                           Email = email,
                                                                                           Password = password
                                                                                        };
      }
   }
}