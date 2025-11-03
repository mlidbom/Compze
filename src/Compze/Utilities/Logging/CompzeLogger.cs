using System;
using System.Threading;
using System.Threading.Tasks;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

// ReSharper disable UnusedMember.Global

namespace Compze.Utilities.Logging;

static class CompzeLogger
{
   // ReSharper disable once UnusedParameter.Global removing the parameter would make it impossible to invoke this as an extension method :)
#pragma warning disable IDE0060 //Reviewed OK: removing the parameter would make it impossible to invoke this as an extension method :)
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

   // ReSharper disable once FieldCanBeMadeReadOnly.Global It is meant to be settable
   internal static LogLevel LogLevel = LogLevel.Info;

   static readonly AsyncLocal<bool> LoggingSuppressedTemporarily = new();
   internal static bool LoggingSuppressed => LoggingSuppressedTemporarily.Value;


   internal static async Task SuppressLoggingWhileRunningAsync(Func<Task> action)
   {
      using(ScopedChange.Enter(() => LoggingSuppressedTemporarily.Value = true, () => LoggingSuppressedTemporarily.Value = false))
      {
         await action().caf();
      }
   }
}