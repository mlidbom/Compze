﻿using System;
using System.Threading.Tasks;
using Compze.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Logging;

static class ExceptionLogger
{
   internal static void ExceptionsAndRethrow(this ILogger log, Action action)
   {
      try
      {
         action();
      }
      catch(Exception e)
      {
         log.Error(e);
         throw;
      }
   }

   internal static void LogAndSuppressExceptions(this ILogger log, Action action)
   {
      try
      {
         action();
      }
      catch(Exception e)
      {
         log.Error(e);
      }
   }

   internal static TResult ExceptionsAndRethrow<TResult>(this ILogger log, Func<TResult> func)
   {
      try
      {
         return func();
      }
      catch(Exception e)
      {
         log.Error(e);
         throw;
      }
   }

   internal static async Task ExceptionsAndRethrowAsync(this ILogger log, Func<Task> action)
   {
      try
      {
         await action().CaF();
      }
      catch(Exception e)
      {
         log.Error(e);
         throw;
      }
   }
}