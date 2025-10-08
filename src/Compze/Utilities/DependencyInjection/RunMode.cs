using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Utilities.DependencyInjection;

class RunMode(bool isTesting) : IRunMode
{
   bool IRunMode.IsTesting { get; } = isTesting;

   public static readonly IRunMode Production = new RunMode(isTesting: false);
}