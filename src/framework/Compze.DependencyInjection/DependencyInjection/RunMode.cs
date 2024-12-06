namespace Compze.DependencyInjection;

class RunMode(bool isTesting) : IRunMode
{
   bool IRunMode.IsTesting { get; } = isTesting;

   public static readonly IRunMode Production = new RunMode(isTesting: false);
}