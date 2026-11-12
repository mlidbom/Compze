using Compze.Must._private.Serialization.Rules;

namespace Compze.Must._private;

abstract class AssertionContext : IAssertionContext
{
   public const string RemoveLine = nameof(RemoveLine);

   public string Expression { get; }

   protected AssertionContext(object? actual, string expression, SerializationRules serializationRules)
   {
      ActualUntyped = actual;
      Expression = this.NormalizeExpressionIndentation(expression);
      SerializationRules = serializationRules;
   }

   object? ActualUntyped { get; }

   public SerializationRules SerializationRules { get; }

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8604 // Possible null reference argument.
   public IAssertionContext<T> Cast<T>() => new AssertionContext<T>((T)ActualUntyped, Expression, SerializationRules);
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

   public static readonly string Separator = "-".Repeat(50).Join();
}

class AssertionContext<T>(T actual, string expression, SerializationRules serializationRules) : AssertionContext(actual, expression, serializationRules), IAssertionContext<T>
{
   public T Actual { get; } = actual;
}
