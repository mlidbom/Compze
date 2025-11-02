using Compze.Utilities.SystemCE;

namespace Compze.Tests.Infrastructure.Fluent;

public static class ObjectNullCheckAssertions
{
   public static Must<TValue> NotBeNull<TValue>(this Must<TValue?> must)
   {
      must.Satisfy(it => it is not null,
                   messageOverride: info => $"""
                                            {must.Separator}
                                            expected the object "it" returned by the expression: 
                                            {must.Separator}
                                            {must.Expression.Indent()}
                                            {must.Separator}
                                            to not be null, but it was null
                                            {must.Separator}
                                            """);
      return must!;
   }

   public static Must<TValue?> BeNull<TValue>(this Must<TValue?> must)
   {
      must.Satisfy(it => it is null,
                   messageOverride: info => $"""
                                            {must.Separator}
                                            expected the object "it" returned by the expression: 
                                            {must.Separator}
                                            {must.Expression.Indent()}
                                            {must.Separator}
                                            to be null, but it was not null
                                            {must.Separator}
                                            """);
      return must;
   }
}
