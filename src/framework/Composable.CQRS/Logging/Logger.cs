using System;
using Composable.SystemCE;
using JetBrains.Annotations;

// ReSharper disable UnusedMember.Global

namespace Composable.Logging;

interface ILogger
{
   ILogger WithLogLevel(LogLevel level);
   VoidCE Error(Exception exception, string? message = null);
   VoidCE Warning(string message);
   VoidCE Warning(Exception exception, string message);
   VoidCE Info(string message);
   VoidCE Debug(string message);
   [StringFormatMethod(formatParameterName: "queuedMessageInformation")]
   VoidCE DebugFormat(string message, params object[] arguments);
}

static class Logger
{
   // ReSharper disable once UnusedParameter.Global removing the parameter would make it impossible to invoke this as an extension method :)
#pragma warning disable IDE0060 //Review OK: removing the parameter would make it impossible to invoke this as an extension method :)
   internal static ILogger Log<T>(this T me) => LogCache<T>.Logger;
#pragma warning restore IDE0060 // Remove unused parameter

   internal static ILogger For<T>() => LogCache<T>.Logger;
   internal static ILogger For(Type loggingType) => LoggerFactoryMethod(loggingType);

   static readonly Func<Type, ILogger> LoggerFactoryMethod = ConsoleLogger.Create;
   static class LogCache<T>
   {
      // ReSharper disable once StaticFieldInGenericType
      public static readonly ILogger Logger = LoggerFactoryMethod(typeof(T));
   }
}