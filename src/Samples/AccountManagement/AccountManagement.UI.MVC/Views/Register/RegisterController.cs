﻿using System;
using AccountManagement.API;
using AccountManagement.Domain.Registration;
using Compze.Messaging.Hypermedia;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AccountManagement.UI.MVC.Views.Register;

public class RegisterController(IRemoteHypermediaNavigator remoteApiNavigator) : ControllerBase
{
   readonly IRemoteHypermediaNavigator _bus = remoteApiNavigator;

   public IActionResult Register(AccountResource.Command.Register registrationCommand)
   {
      if(!ModelState.IsValid) return View("RegistrationForm");

      var result = registrationCommand.PostOn(_bus);
      switch(result.Status)
      {
         case RegistrationAttemptStatus.Successful:
            return View("ValidateYourEmail", result.RegisteredAccount);
         case RegistrationAttemptStatus.EmailAlreadyRegistered:
            ModelState.AddModelError((AccountResource.Command.Register model) => model.Email, "Email is already registered");
            ModelState.Remove((AccountResource.Command.Register model) => model.MessageId);
            registrationCommand.ReplaceDeduplicationId();
            return View("RegistrationForm", registrationCommand);
         default:
            throw new ArgumentOutOfRangeException();
      }
   }

   public IActionResult RegistrationForm() => View("RegistrationForm", Api.Accounts.Command.Register().NavigateOn(_bus));
}