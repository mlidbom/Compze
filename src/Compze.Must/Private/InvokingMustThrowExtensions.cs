namespace Compze.Must.Private;

static class InvokingMustThrowExtensions
{
   public static string ThrowAssertionFailureHeading(this IAssertionContext<Func<Task>> context, Type expectedException)
   {
      return $"""
              {AssertionContext.Separator}
              Failing assertion:
              {AssertionContext.Separator}
              InvokingAsync({context.Expression}).Must().ThrowAsync<{expectedException.Name}>()
              {AssertionContext.Separator}
              """;
   }

   public static string ThrowAssertionFailureHeading(this IAssertionContext<Action> context, Type expectedException)
   {
      return $"""
              {AssertionContext.Separator}
              Failing assertion:
              {AssertionContext.Separator}
              Invoking({context.Expression}).Must().Throw<{expectedException.Name}>()
              {AssertionContext.Separator}
              """;
   }
}
