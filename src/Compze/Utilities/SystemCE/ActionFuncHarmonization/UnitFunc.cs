using System;
using System.Threading.Tasks;
using Compze.Utilities.Functional;

namespace Compze.Utilities.SystemCE.ActionFuncHarmonization;

static class UnitFunc
{
   internal static Func<unit> From(Action action) => action.AsUnitFunc();
   internal static Func<TParam, unit> From<TParam>(Action<TParam> action) => action.AsUnitFunc();
   internal static Func<TParam, TParam2, unit> From<TParam, TParam2>(Action<TParam,TParam2> action) => action.AsUnitFunc();


   internal static Func<Task<unit>> From(Func<Task> action) => action.AsUnitFunc();
   internal static Func<TParam, Task<unit>> From<TParam>(Func<TParam, Task> action) => action.AsUnitFunc();
   internal static Func<TParam, TParam2, Task<unit>> From<TParam, TParam2>(Func<TParam,TParam2, Task> action) => action.AsUnitFunc();
}