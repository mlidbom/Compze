﻿using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace AccountManagement.Domain;

///<summary>
/// A small value object that ensures that it is impossible to create an invalid email.
/// This frees all users of the class from ever having to validated an email.
/// As long as it is not null it is guaranteed to be valid.
/// </summary>
public record Email
{
   static readonly Regex BasicEmailValidationRegex = new(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,4}$");
   [JsonProperty] public string StringValue { get; }

   public override string ToString() => StringValue;

   [JsonConstructor] Email(string stringValue)
   {
      AssertIsValid(stringValue);
      StringValue = stringValue;
   }

   public static bool IsValidEmail(string emailAddress)
   {
      if(string.IsNullOrWhiteSpace(emailAddress)) return false;
      if(!BasicEmailValidationRegex.IsMatch(emailAddress)) return false;
      if(emailAddress.Contains("..", StringComparison.Ordinal)) return false;
      // ReSharper disable once ConvertIfStatementToReturnStatement
      if(emailAddress.Contains("@.", StringComparison.Ordinal) || emailAddress.Contains(".@", StringComparison.Ordinal)) return false;

      return true;
   }

   public static Email Parse(string emailAddress) => new(emailAddress);

   //Note how all the exceptions contain the invalid email address. Always make sure that exceptions contain the relevant information.
   static void AssertIsValid(string emailAddress)
   {
      if(!IsValidEmail(emailAddress))
      {
         throw new InvalidEmailException(emailAddress);
      }
   }
}

public class InvalidEmailException : ArgumentException
{
   // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
   internal InvalidEmailException(string message) : base($"Supplied string: '{message ?? "[null]"}'") {}
}