using System.ComponentModel.DataAnnotations;
using AccountManagement.API.ValidationAttributes;
using Compze.Core.Public;
using Compze.Core.Tessaging.Public;

// ReSharper disable MemberCanBeMadeStatic.Global

namespace AccountManagement.API;

public partial class AccountResource
{
   public static partial class Tommand
   {
      public partial class LogIn : TessageTypes.Remotable.AtMostOnce.AtMostOnceTypermediaTommand<LogIn.LoginAttemptResult>
      {
         public static LogIn Create() => new() {Id = new TessageId()};

         [Required] [Email] public string Email { get; init; } = string.Empty;
         [Required] public string Password { get; init; } = string.Empty;

         public LogIn WithValues(string email, string password) => new()
                                                                   {
                                                                      Id = Id,
                                                                      Email = email,
                                                                      Password = password
                                                                   };
      }
   }
}