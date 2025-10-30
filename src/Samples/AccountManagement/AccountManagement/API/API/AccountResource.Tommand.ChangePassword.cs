using AccountManagement.API.ValidationAttributes;
using Compze.Core.Public;
using Compze.Core.Tessaging.Public;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AccountManagement.API;

public partial class AccountResource
{
   public static partial class Tommand
   {
      public class ChangePassword : TessageTypes.Remotable.AtMostOnce.AtMostOnceTypermediaTommand, IValidatableObject
      {
         [UsedImplicitly] public ChangePassword() : base() {}
         public ChangePassword(TaggregateId accountId):base() => AccountId = accountId;

         [Required] [TaggregateId] public TaggregateId AccountId { get; set; }
         [Required] public string OldPassword { get; set; } = string.Empty;
         [Required] public string NewPassword { get; set; } = string.Empty;

         public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Domain.Passwords.Password.Policy.Validate(NewPassword, this, () => NewPassword);

         public ChangePassword WithValues(string oldPassword, string newPassword) => new(AccountId)
                                                                                     {
                                                                                        OldPassword = oldPassword,
                                                                                        NewPassword = newPassword
                                                                                     };
      }
   }
}