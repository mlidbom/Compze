﻿using System;

namespace AccountManagement.Domain.Passwords;

///<summary>Thrown if an attempt is made to authenticate with a password that does not match the password for the account.</summary>
public class WrongPasswordException : Exception {}