using AccountManagement.API.ValidationAttributes;
using AccountManagement.Domain;
using Compze.Core.Tessaging.Public;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace AccountManagement.API;

public partial class AccountResource
{
   public static partial class Tommand
   {
      public class ChangeEmail : TessageTypes.Remotable.AtMostOnce.AtMostOnceTypermediaTommand
      {
         [JsonConstructor]public ChangeEmail(AccountId accountId, string email) : base()
         {
            AccountId = accountId;
            Email = email;
         }

         internal ChangeEmail(AccountId accountId):base() => AccountId = accountId;

         [Required] [TaggregateId] public AccountId AccountId { get; set; }
         [Required] [Email] public string Email { get; set; } = string.Empty;

         public ChangeEmail WithEmail(string email) => new(AccountId)
                                                       {
                                                          Email = email
                                                       };
      }
   }
}