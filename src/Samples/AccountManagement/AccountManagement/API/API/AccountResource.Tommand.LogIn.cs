using System;
using System.ComponentModel.DataAnnotations;
using AccountManagement.API.ValidationAttributes;
using Compze.Abstractions.Tessaging.Public;

// ReSharper disable MemberCanBeMadeStatic.Global

namespace AccountManagement.API;

public partial class AccountResource
{
   public static partial class Tommand
   {
      public partial class LogIn() : TessageTypes.Remotable.AtMostOnce.AtMostOnceTommand<LogIn.LoginAttemptResult>(DeduplicationIdHandling.Reuse)
      {
         public static LogIn Create() => new() {TessageId = Guid.CreateVersion7()};

         [Required] [Email] public string Email { get; set; } = string.Empty;
         [Required] public string Password { get; set; } = string.Empty;

         public LogIn WithValues(string email, string password) => new()
                                                                   {
                                                                      TessageId = TessageId,
                                                                      Email = email,
                                                                      Password = password
                                                                   };
      }
   }
}