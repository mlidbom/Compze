using AccountManagement.API;
using AccountManagement.Domain.Registration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Compze.Typermedia;

namespace AccountManagement.UI.MVC.Views.Register;

public class RegisterController(IRemoteTypermediaNavigator remoteApiNavigator) : ControllerBase
{
   readonly IRemoteTypermediaNavigator _bus = remoteApiNavigator;

   public IActionResult Register(AccountResource.Tommand.Register registrationTommand)
   {
      if(!ModelState.IsValid) return View("RegistrationForm");

      var result = registrationTommand.PostOn(_bus);
      switch(result.Status)
      {
         case RegistrationAttemptStatus.Successful:
            return View("ValidateYourEmail", result.RegisteredAccount);
         case RegistrationAttemptStatus.EmailAlreadyRegistered:
            ModelState.AddModelError((AccountResource.Tommand.Register model) => model.Email, "Email is already registered");
            ModelState.Remove((AccountResource.Tommand.Register model) => model.Id);
            return View("RegistrationForm", registrationTommand);
         default:
            throw new ArgumentOutOfRangeException();
      }
   }

   public IActionResult RegistrationForm() => View("RegistrationForm", Api.Accounts.Tommand.Register().NavigateOn(_bus));
}