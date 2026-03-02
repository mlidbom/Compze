namespace Compze.Utilities.Testing.Must;

public class ExpressionValue(string expression, object? value)
{
   internal string Expression { get; } = expression;
   internal object? Value { get; } = value;
}
