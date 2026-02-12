using System;
using System.Threading;
using System.Threading.Tasks;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

// ReSharper disable UnusedMember.Global

namespace Compze.Utilities.Logging;

public static class CompzeLogger
{
   // ReSharper disable once UnusedParameter.Global removing the parameter would make it impossible to invoke this as an extension method :)
#pragma warning disable IDE0060 //Reviewed OK: removing the parameter would make it impossible to invoke this as an extension method :)
   public static ILogger Log<T>(this T me) => LogCache<T>.Logger;
#pragma warning restore IDE0060 // Remove unused parameter

   public static ILogger For<T>() => LogCache<T>.Logger;
   public static ILogger For(Type loggingType) => LoggerFactoryMethod(loggingType);

   public static Func<Type, ILogger> LoggerFactoryMethod = ConsoleLogger.Create;
   public static class LogCache<T>
   {
      // ReSharper disable once StaticFieldInGenericType
      public static readonly ILogger Logger = LoggerFactoryMethod(typeof(T));
   }

   // ReSharper disable once FieldCanBeMadeReadOnly.Global It is meant to be settable
   public static LogLevel LogLevel = LogLevel.Info;

   static readonly AsyncLocal<bool> LoggingSuppressedTemporarily = new();
   public static bool LoggingSuppressed => LoggingSuppressedTemporarily.Value;


   public static async Task SuppressLoggingWhileRunningAsync(Func<Task> action)
   {
      using(ScopedChange.Enter(() => LoggingSuppressedTemporarily.Value = true, () => LoggingSuppressedTemporarily.Value = false))
      {
         await action().caf();
      }
   }
}