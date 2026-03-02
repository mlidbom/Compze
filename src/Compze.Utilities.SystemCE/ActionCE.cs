using System;
using System.Collections.Generic;
using Compze.Utilities.SystemCE.LinqCE;

namespace Compze.Utilities.SystemCE;

static class ActionCE
{
   public static void InvokeAll(this IEnumerable<Action> @this) => @this.ForEach(me => me.Invoke());

   public static readonly Action NullOp = () => {};
}