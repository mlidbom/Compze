namespace Compze.Utilities.Testing.Must;
// ReSharper disable InconsistentNaming

public static class Must___TypeOfActual
{
   public static IAssertionContext<TExpected> BeExactType<TExpected>(this IAssertionContext context) =>
      context.SatisfyInternal(it => it.GetType() == typeof(TExpected), caller: CallName.For<TExpected>())
          .Cast<TExpected>();

   public static IAssertionContext<TExpected> BeAssignableTo<TExpected>(this IAssertionContext context) =>
      context.SatisfyInternal(it => it.GetType().IsAssignableTo(typeof(TExpected)), caller: CallName.For<TExpected>())
          .Cast<TExpected>();
}