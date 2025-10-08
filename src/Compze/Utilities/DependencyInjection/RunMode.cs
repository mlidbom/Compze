using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Utilities.DependencyInjection;

//Todo: Doubtful about this being a public class living in utilities. Feels halfway Compze internal?
public class RunMode(bool isTesting) : IRunMode
{
   bool IRunMode.IsTesting { get; } = isTesting;

   public static readonly IRunMode Production = new RunMode(isTesting: false);
}