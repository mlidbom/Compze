﻿using System;
using System.ComponentModel.DataAnnotations;
using AccountManagement.API.ValidationAttributes;
using Compze.Messaging;

// ReSharper disable MemberCanBeMadeStatic.Global

namespace AccountManagement.API;

public partial class AccountResource
{
   public static partial class Command
   {
      public partial class LogIn() : MessageTypes.Remotable.AtMostOnce.AtMostOnceCommand<LogIn.LoginAttemptResult>(DeduplicationIdHandling.Reuse)
      {
         public static LogIn Create() => new() {MessageId = Guid.NewGuid()};

         [Required] [Email] public string Email { get; set; } = string.Empty;
         [Required] public string Password { get; set; } = string.Empty;

         public LogIn WithValues(string email, string password) => new()
                                                                   {
                                                                      MessageId = MessageId,
                                                                      Email = email,
                                                                      Password = password
                                                                   };
      }
   }
}