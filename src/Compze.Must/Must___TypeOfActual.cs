namespace Compze.Must;
// ReSharper disable InconsistentNaming
/// <summary>Assertions about the runtime type of the value.</summary>
public static class Must___TypeOfActual
{
   /// <summary>Asserts that the value's runtime type is exactly <typeparamref name="TExpected"/>, narrowing the context to it.</summary>
   public static IAssertionContext<TExpected> BeExactType<TExpected>(this IAssertionContext context) =>
      context.RunAssertion(it => it.GetType() == typeof(TExpected), caller: CallName.For<TExpected>())
             .Cast<TExpected>();

   /// <summary>Asserts that the value is assignable to <typeparamref name="TExpected"/>, narrowing the context to it.</summary>
   public static IAssertionContext<TExpected> BeAssignableTo<TExpected>(this IAssertionContext context) =>
      context.RunAssertion(it => it.GetType().IsAssignableTo(typeof(TExpected)), caller: CallName.For<TExpected>())
             .Cast<TExpected>();
}
