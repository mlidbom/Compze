﻿using System;
using Composable.Functional;

// ReSharper disable UnusedMember.Global

namespace Composable.Logging;

interface ILogger
{
   ILogger WithLogLevel(LogLevel level);
   Unit Error(Exception exception, string? message = null);
   Unit Warning(string message);
   Unit Warning(Exception exception, string message);
   Unit Info(string message);
   Unit Debug(string message);
}

static class ComposableLogger
{
   // ReSharper disable once UnusedParameter.Global removing the parameter would make it impossible to invoke this as an extension method :)
#pragma warning disable IDE0060 //Review OK: removing the parameter would make it impossible to invoke this as an extension method :)
   internal static ILogger Log<T>(this T me) => LogCache<T>.Logger;
#pragma warning restore IDE0060 // Remove unused parameter

   internal static ILogger For<T>() => LogCache<T>.Logger;
   internal static ILogger For(Type loggingType) => LoggerFactoryMethod(loggingType);

   internal static Func<Type, ILogger> LoggerFactoryMethod = ConsoleLogger.Create;
   static class LogCache<T>
   {
      // ReSharper disable once StaticFieldInGenericType
      public static readonly ILogger Logger = LoggerFactoryMethod(typeof(T));
   }
}