﻿using AccountManagement.API;
using Microsoft.AspNetCore.Mvc;

namespace AccountManagement.UI.MVC.Views;

public class ControllerBase : Controller
{
    protected CompositeApi Api => new();
}

public class CompositeApi
{
    public AccountApi Accounts => AccountApi.Instance;
}