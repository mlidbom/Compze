﻿using System;
using System.Collections.Generic;
using Compze.SystemCE.CollectionsCE.GenericCE;

namespace Compze.SystemCE;

public class StrictAggregateDisposable : StrictlyManagedResourceBase<StrictAggregateDisposable>
{
   readonly IList<IDisposable> _managedResources = new List<IDisposable>();

   public static StrictAggregateDisposable Create(params IDisposable[] disposables) => new(disposables);

   StrictAggregateDisposable(params IDisposable[] disposables) => Add(disposables);

   void Add(params IDisposable[] disposables) => _managedResources.AddRange(disposables);

   protected override void Dispose(bool disposing)
   {
      foreach (var managedResource in _managedResources)
      {
         managedResource.Dispose();
      }
      _managedResources.Clear();
      base.Dispose(disposing);
   }
}