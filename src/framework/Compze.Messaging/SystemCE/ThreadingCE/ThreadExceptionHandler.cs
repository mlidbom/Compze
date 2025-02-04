﻿using System;
using System.Threading;
using Compze.Logging;

namespace Compze.SystemCE.ThreadingCE;

// ReSharper disable once ClassNeverInstantiated.Global
class ThreadExceptionHandler
{
   //Bug: We need something much better than this which lets us surface exceptions to tests and endpoints etc in a sane fashion. Things should not just stop happening and we should not just swallow exceptions either.
   //Probably something like our own thread manager. That start and tracks all threads and that guarantees, on Stop/Dispose, that all threads are stopped sanely and has caused no exceptions.
   //If they have caused exception, rethrow them on the main test thread.
   internal static ThreadStart WrapThreadStart(ThreadStart start) =>
      () =>
      {
         try
         {
            start.Invoke();
         }
         catch(Exception exception) when(exception is OperationCanceledException or ThreadInterruptedException or ThreadAbortException)
         {
            CompzeLogger.For<ThreadExceptionHandler>().Info($"Thread: {Thread.CurrentThread.Name} is terminating because it received a: {exception.GetType().Name}.");
         }
         catch(Exception exception)
         {
            CompzeLogger.For<ThreadExceptionHandler>().Error(exception, $"Error occured on background poller thread: {Thread.CurrentThread.Name}. Thread is no longer running.");
         }
      };
}