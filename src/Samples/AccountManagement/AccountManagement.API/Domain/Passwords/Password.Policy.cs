﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using AccountManagement.API;
using AccountManagement.Extensions;
using Compze.SystemCE.ComponentModelCE.DataAnnotationsCE;

namespace AccountManagement.Domain.Passwords;

public partial class Password
{
#pragma warning disable CA1724 // Type names should not match namespaces
   public static class Policy
#pragma warning restore CA1724 // Type names should not match namespaces
   {
      ///<summary><para>returns a list of the ways in which a specific password fails to fulfill the policy. If the list is empty the password is compliant with the policy.</para> </summary>
      public static IEnumerable<Failures> GetPolicyFailures(string? password)
      {
         if(password == null)
         {
            return [Failures.Null]; //Everything else will fail with null reference exception if we don't return here...
         }

         var failures = new List<Failures>();
         //Create a simple extension to keep the code short an expressive and DRY. If AddIf is unclear just hover your pointer over the method and the documentation comment should clear everything up.
         failures.AddIf(password.Length < 4, Failures.ShorterThanFourCharacters);
         failures.AddIf(password.Trim() != password, Failures.BorderedByWhitespace);
#pragma warning disable CA1862 //This is not a case-insensitive comparison.
#pragma warning disable CA1308 //This is not a case-insensitive comparison.
         failures.AddIf(password.ToLowerInvariant() == password, Failures.MissingUppercaseCharacter);
         failures.AddIf(password.ToUpperInvariant() == password, Failures.MissingLowerCaseCharacter);
#pragma warning disable CA1308 //This is not a case-insensitive comparison.
#pragma warning restore CA1862
         return failures;
      }

      internal static void AssertPasswordMatchesPolicy(string password)
      {
         var passwordPolicyFailures = GetPolicyFailures(password).ToList();
         if(passwordPolicyFailures.Any())
         {
            //Don't throw a generic exception or ArgumentException. Throw a specific type that let's clients make use of it easily and safely.
            throw new PasswordDoesNotMatchPolicyException(passwordPolicyFailures);
            //Normally we would include the value to make debugging easier but not for passwords since that would be a security issue. We do make sure to include HOW it was invalid.
         }
      }

      [Flags]
      public enum Failures
      {
         Null = 1, //Make sure all values are powers of 2 so that the flags can be combined freely.
         MissingUppercaseCharacter = 2,
         MissingLowerCaseCharacter = 4,
         ShorterThanFourCharacters = 8,
         BorderedByWhitespace = 16
      }

      // ReSharper disable once MemberHidesStaticFromOuterClass
      internal static IEnumerable<ValidationResult> Validate(string password, IValidatableObject owner, Expression<Func<object>> passwordMember)
      {
         var policyFailures = Policy.GetPolicyFailures(password).ToList();
         if (policyFailures.Any())
         {
            yield return policyFailures.First() switch
            {
               Policy.Failures.BorderedByWhitespace => owner.CreateValidationResult(RegisterAccountCommandResources.Password_BorderedByWhitespace, passwordMember),
               Policy.Failures.MissingLowerCaseCharacter => owner.CreateValidationResult(RegisterAccountCommandResources.Password_MissingLowerCaseCharacter, passwordMember),
               Policy.Failures.MissingUppercaseCharacter => owner.CreateValidationResult(RegisterAccountCommandResources.Password_MissingUpperCaseCharacter, passwordMember),
               Policy.Failures.ShorterThanFourCharacters => owner.CreateValidationResult(RegisterAccountCommandResources.Password_ShorterThanFourCharacters, passwordMember),
               Policy.Failures.Null => throw new Exception("Null should have been caught by the Required attribute"),
               _ => throw new Exception($"Unknown password failure type {policyFailures.First()}")
            };
         }
      }
   }
}