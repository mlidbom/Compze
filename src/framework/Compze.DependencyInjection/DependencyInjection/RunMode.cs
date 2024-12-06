namespace Compze.DependencyInjection;

class RunMode(bool isTesting) : IRunMode
{
   readonly bool _isTesting = isTesting;
   bool IRunMode.IsTesting => _isTesting;

   public static readonly IRunMode Production = new RunMode(isTesting: false);
}