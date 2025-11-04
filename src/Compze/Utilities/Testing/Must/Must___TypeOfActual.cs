namespace Compze.Utilities.Testing.Must;
// ReSharper disable InconsistentNaming

public static class Must___TypeOfActual
{
   //todo: rename
   public static IMust<TExpected> BeOfType<TExpected>(this IMust must) =>
      must.SatisfyInternal(it => it.GetType() == typeof(TExpected), callerName: CallName.For<TExpected>())
          .Cast<TExpected>();

   public static IMust<TExpected> BeAssignableTo<TExpected>(this IMust must) =>
      must.SatisfyInternal(it => it.GetType().IsAssignableTo(typeof(TExpected)), callerName: CallName.For<TExpected>())
          .Cast<TExpected>();
}