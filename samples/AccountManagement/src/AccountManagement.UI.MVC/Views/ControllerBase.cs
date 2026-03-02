using AccountManagement.API;
using Microsoft.AspNetCore.Mvc;
// ReSharper disable MemberCanBeMadeStatic.Global

namespace AccountManagement.UI.MVC.Views;

public class ControllerBase : Controller
{
   protected CompositeApi Api => new();
}

public class CompositeApi
{
   internal AccountApi Accounts => AccountApi.Instance;
}