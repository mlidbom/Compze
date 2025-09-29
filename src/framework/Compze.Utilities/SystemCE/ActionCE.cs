using System;
using System.Collections.Generic;
using Compze.SystemCE.LinqCE;

namespace Compze.SystemCE;

static class ActionCE
{
   internal static void InvokeAll(this IEnumerable<Action> @this) => @this.ForEach(me => me.Invoke());
}