using System.Runtime.CompilerServices;

namespace Compze.Must.Private;

static class CallName
{
   public static string For<T>([CallerMemberName] string? caller = null) => $"{caller}<{typeof(T).Name}>";
}
