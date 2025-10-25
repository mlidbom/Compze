using Newtonsoft.Json;

namespace AccountManagement.API;

public partial class AccountResource
{
   public class AccountTommands
   {
#pragma warning disable IDE0051 // Remove unused private members
      [JsonConstructor]AccountTommands(Tommand.ChangeEmail changeEmail, Tommand.ChangePassword changePassword)
#pragma warning restore IDE0051 // Remove unused private members
      {
         ChangeEmail = changeEmail;
         ChangePassword = changePassword;
      }

      public AccountTommands(AccountResource accountResource)
      {
         ChangeEmail = new Tommand.ChangeEmail(accountResource.Id);
         ChangePassword = new Tommand.ChangePassword(accountResource.Id);
      }

      public Tommand.ChangeEmail ChangeEmail { get; private set; }

      public Tommand.ChangePassword ChangePassword { get; private set; }
   }
}