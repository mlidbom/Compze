using System.Diagnostics.CodeAnalysis;

namespace Compze.SystemCE;

// ReSharper disable once UnusedType.Global
///<summary>
/// This stays here only to hold this comment.
/// The idea is that we could declare a reusable Func type that works like Func, but allows for using out parameters.
/// This sounds like it could simplify various scenarios using out parameters. But it ultimately does not work because:
/// 1. Lambdas and anonymous methods cannot use out or ref parameters due to how they capture variables and manage scope,
/// 2. Pretty much the whole point of a shared Func type is the ability to pass lambdas too methods taking this Func type.
/// So this whole idea is a dead end, and if you are reading this comment, hopefully it saved you from one more go-round of trying to make this work.
/// It won't work. Give up ;)
/// </summary>
delegate bool OutVariableFuncsDoNotWorkDontTryToImplementItAgain<in T, TResult>(T input, [NotNullWhen(true)]out TResult output);
