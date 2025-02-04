﻿using System.ComponentModel;
using AccountManagement.Domain.Registration;
using Newtonsoft.Json;

// ReSharper disable MemberCanBeMadeStatic.Global Because we want these members to be accessed through the fluent API we don't want to make them static.

namespace AccountManagement.API;

public partial class AccountResource
{
   public static partial class Command
   {
      public partial class Register
      {
         public class RegistrationAttemptResult
         {
            [JsonConstructor]internal RegistrationAttemptResult(RegistrationAttemptStatus status, AccountResource? registeredAccount)
            {
               if(status == RegistrationAttemptStatus.Successful && registeredAccount is null) throw new InvalidEnumArgumentException("Status cannot be successful and registered account null");
               Status = status;
               RegisteredAccount = registeredAccount;
            }

            public RegistrationAttemptStatus Status { get; private set; }
            public AccountResource? RegisteredAccount { get; private set; }
         }
      }
   }
}