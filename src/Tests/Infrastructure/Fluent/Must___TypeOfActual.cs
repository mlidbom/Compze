namespace Compze.Tests.Infrastructure.Fluent;
// ReSharper disable InconsistentNaming

public static class Must___TypeOfActual
{
   public static IMust<TExpected> BeExactType<TExpected>(this IMust must) =>
      must.Satisfy(it => it.GetType() == typeof(TExpected))
          .Cast<TExpected>();

   public static IMust<TExpected> BeAssignableTo<TExpected>(this IMust must) =>
      must.Satisfy(it => it.GetType().IsAssignableTo(typeof(TExpected)))
          .Cast<TExpected>();
}
