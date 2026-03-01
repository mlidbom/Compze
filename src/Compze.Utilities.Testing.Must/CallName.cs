using System.Runtime.CompilerServices;

namespace Compze.Utilities.Testing.Must;

internal static class CallName
{
   public static string For<T>([CallerMemberName] string? caller = null) => $"{caller}<{typeof(T).Name}>";
}
