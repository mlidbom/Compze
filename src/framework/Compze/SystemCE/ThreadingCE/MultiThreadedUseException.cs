﻿using System;
using System.Threading;
using Compze.Contracts.Deprecated;

namespace Compze.SystemCE.ThreadingCE;

///<summary>Thrown if the <see cref="SingleThreadUseGuard"/> detects a thread change.</summary>
public class MultiThreadedUseException : InvalidOperationException
{
   ///<summary>Constructs an instance using the supplied arguments to create an informative queuedMessageInformation.</summary>
   internal MultiThreadedUseException(object guarded, Thread owningThread, Thread currentThread)
      : base($"Attempt to use {guarded} from thread Id:{currentThread.ManagedThreadId}, Name: {currentThread.Name} when owning thread was Id: {owningThread.ManagedThreadId} Name: {owningThread.Name}") =>
      Contracts.Assert.Argument.NotNull(guarded).NotNull(owningThread).NotNull(currentThread);
}