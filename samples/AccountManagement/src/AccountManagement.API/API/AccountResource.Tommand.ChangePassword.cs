using AccountManagement.API.ValidationAttributes;
using AccountManagement.Domain;
using System.ComponentModel.DataAnnotations;
using Compze.Tessaging.Abstractions.TessageTypes;

// ReSharper disable PropertyCanBeMadeInitOnly.Local

namespace AccountManagement.API;

public partial class AccountResource
{
   public static partial class Tommand
   {
      public class ChangePassword : Remotable.AtMostOnce.AtMostOnceTypermediaTommand, IValidatableObject
      {
         [Obsolete("Used by serializer", error:true)]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
         public ChangePassword() {}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
         public ChangePassword(AccountId accountId) => AccountId = accountId;

         [Required] [TaggregateId] public AccountId AccountId { get; set; }
         [Required] public string OldPassword { get; private set; } = string.Empty;
         [Required] public string NewPassword { get; private set; } = string.Empty;

         public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Domain.Passwords.Password.Policy.Validate(NewPassword, this, () => NewPassword);

         public ChangePassword WithValues(string oldPassword, string newPassword) => new(AccountId)
                                                                                     {
                                                                                        OldPassword = oldPassword,
                                                                                        NewPassword = newPassword
                                                                                     };
      }
   }
}