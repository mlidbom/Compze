﻿using System;
using Composable.SystemCE.ThreadingCE.ResourceAccess;

namespace Composable.GenericAbstractions;

public class OptimizedInitializer
{
   readonly MonitorCE _monitor = MonitorCE.WithDefaultTimeout();
   readonly Action _initialize;

   internal void EnsureInitialized()
   {
      if(!IsInitialized)
      {
         _monitor.Update(() =>
         {
            if(!IsInitialized)
            {
               _initialize();
               IsInitialized = true;
            }
         });
      }
   }

   public bool IsInitialized { get; private set; }

   internal OptimizedInitializer(Action initialize) => _initialize = initialize;
}