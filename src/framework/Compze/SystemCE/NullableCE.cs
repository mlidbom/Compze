﻿using System;

namespace Compze.SystemCE;

public static class NullableCE
{
   public static T NotNull<T>(this T? @this) where T : class => @this ?? throw new ArgumentNullException(nameof(@this));
}