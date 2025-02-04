﻿using System;
using System.Threading.Tasks;

namespace Compze.SystemCE.ThreadingCE.TasksCE;

static class TaskCEExceptionHandling
{
   public static async Task WithAggregateExceptions(this Task task)
   {
      try
      {
         await task.AsUnit().CaF();
      }
      catch(Exception exception)
      {
         throw task.Exception ?? new AggregateException(exception);
      }
   }

   public static async Task WithAggregateExceptions(this ValueTask valueTask) => await valueTask.AsTask().WithAggregateExceptions().CaF();
}
