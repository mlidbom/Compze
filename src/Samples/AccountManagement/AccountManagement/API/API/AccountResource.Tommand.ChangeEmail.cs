using System;
using System.ComponentModel.DataAnnotations;
using AccountManagement.API.ValidationAttributes;
using Compze.Core.Tessaging.Public;
using Newtonsoft.Json;

namespace AccountManagement.API;

public partial class AccountResource
{
   public static partial class Tommand
   {
      public class ChangeEmail : TessageTypes.Remotable.AtMostOnce.AtMostOnceHypermediaTommand
      {
         [JsonConstructor]public ChangeEmail(Guid accountId, string email) : base(DeduplicationIdHandling.Reuse)
         {
            AccountId = accountId;
            Email = email;
         }

         internal ChangeEmail(Guid accountId):base(DeduplicationIdHandling.Create) => AccountId = accountId;

         [Required] [EntityId] public Guid AccountId { get; set; }
         [Required] [Email] public string Email { get; set; } = string.Empty;

         public ChangeEmail WithEmail(string email) => new(AccountId)
                                                       {
                                                          Email = email
                                                       };
      }
   }
}