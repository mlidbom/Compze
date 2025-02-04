﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using Newtonsoft.Json;

namespace AccountManagement.Domain.Passwords;

/// <summary>Note how all the business logic of a password is encapsulated and the instance is immutable after being created.</summary>
public partial class Password
{
   readonly byte[] _hash;
   readonly byte[] _salt;
   public byte[] GetHash() => _hash;
   public byte[] GetSalt() => _salt;

#pragma warning disable IDE0051 // Remove unused private members
   [JsonConstructor]Password(byte[] hash, byte[] salt)
   {
      _hash = hash;
      _salt = salt;
   }
#pragma warning restore IDE0051 // Remove unused private members

   public Password(string password)
   {
      Policy.AssertPasswordMatchesPolicy(password); //Use a nested class to make the policy easily discoverable while keeping this class short and expressive.
      _salt = Guid.NewGuid().ToByteArray();
      _hash = PasswordHasher.HashPassword(salt: GetSalt(), password: password);
   }

   public bool IsCorrectPassword(string password) => GetHash().SequenceEqual(PasswordHasher.HashPassword(GetSalt(), password));

   public void AssertIsCorrectPassword(string attemptedPassword)
   {
      if(!IsCorrectPassword(attemptedPassword))
      {
         throw new WrongPasswordException();
      }
   }

   public static IEnumerable<ValidationResult> Validate(string password, IValidatableObject owner, Expression<Func<object>> passwordMember) => Policy.Validate(password, owner, passwordMember);
}