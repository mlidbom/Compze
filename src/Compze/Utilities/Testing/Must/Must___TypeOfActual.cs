namespace Compze.Utilities.Testing.Must;
// ReSharper disable InconsistentNaming

public static class Must___TypeOfActual
{
   //todo: rename
   public static IAssertionContext<TExpected> BeOfType<TExpected>(this IAssertionContext context) =>
      context.SatisfyInternal(it => it.GetType() == typeof(TExpected), callerName: CallName.For<TExpected>())
          .Cast<TExpected>();

   public static IAssertionContext<TExpected> BeAssignableTo<TExpected>(this IAssertionContext context) =>
      context.SatisfyInternal(it => it.GetType().IsAssignableTo(typeof(TExpected)), callerName: CallName.For<TExpected>())
          .Cast<TExpected>();
}