using AccountManagement.API;
using Compze.Core.Tessaging.Typermedia.Public;
using Compze.Tessaging.Abstractions.Tessaging.Typermedia.Public;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AccountManagement.UI.MVC.Views.Login;

public class LoginController(IRemoteTypermediaNavigator remoteApiNavigator) : ControllerBase
{
   readonly IRemoteTypermediaNavigator _bus = remoteApiNavigator;

   public IActionResult Login(AccountResource.Tommand.LogIn loginTommand)
   {
      if(!ModelState.IsValid) return View("LoginForm");

      var result = loginTommand.PostOn(_bus);
      if(result.Succeeded)
      {
         return View("LoggedIn");
      }

      ModelState.AddModelError("Something", "Login Failed");
      ModelState.Remove((AccountResource.Tommand.LogIn model) => model.Id);
      return View("LoginForm", loginTommand);
   }

   public IActionResult LoginForm() => View("LoginForm", _bus.Navigate(Api.Accounts.Tommand.Login()));
}