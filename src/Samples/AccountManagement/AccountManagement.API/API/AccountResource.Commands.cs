using Newtonsoft.Json;

namespace AccountManagement.API;

public partial class AccountResource
{
   public class AccountCommands
   {
#pragma warning disable IDE0051 // Remove unused private members
      [JsonConstructor]AccountCommands(Command.ChangeEmail changeEmail, Command.ChangePassword changePassword)
#pragma warning restore IDE0051 // Remove unused private members
      {
         ChangeEmail = changeEmail;
         ChangePassword = changePassword;
      }

      public AccountCommands(AccountResource accountResource)
      {
         ChangeEmail = new Command.ChangeEmail(accountResource.Id);
         ChangePassword = new Command.ChangePassword(accountResource.Id);
      }

      public Command.ChangeEmail ChangeEmail { get; private set; }

      public Command.ChangePassword ChangePassword { get; private set; }
   }
}