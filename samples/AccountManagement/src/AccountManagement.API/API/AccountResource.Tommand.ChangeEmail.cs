using AccountManagement.API.ValidationAttributes;
using AccountManagement.Domain;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using Compze.Tessaging;
using Compze.Tessaging.TessageTypes;

// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace AccountManagement.API;

public partial class AccountResource
{
   public static partial class Tommand
   {
      public class ChangeEmail : Remotable.AtMostOnce.AtMostOnceTypermediaTommand
      {
         [JsonConstructor]public ChangeEmail(AccountId accountId, string email)
         {
            AccountId = accountId;
            Email = email;
         }

         internal ChangeEmail(AccountId accountId) => AccountId = accountId;

         [Required] [TaggregateId] public AccountId AccountId { get; private set; }
         [Required] [Email] public string Email { get; private set; } = string.Empty;

         public ChangeEmail WithEmail(string email) => new(AccountId)
                                                       {
                                                          Email = email
                                                       };
      }
   }
}