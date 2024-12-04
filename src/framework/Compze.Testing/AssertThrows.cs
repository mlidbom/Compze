using System;
using System.Threading.Tasks;
using Compze.SystemCE.ThreadingCE.TasksCE;
using FluentAssertions;

namespace Compze.Testing;

public static class AssertThrows
{
   public static async Task<TException> Async<TException>([JetBrains.Annotations.InstantHandle] Func<Task> action) where TException : Exception =>
      (await FluentActions.Invoking(action).Should().ThrowAsync<TException>().CaF()).Which;
}
