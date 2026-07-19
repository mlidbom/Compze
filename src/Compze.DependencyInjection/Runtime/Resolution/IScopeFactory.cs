namespace Compze.DependencyInjection.Runtime.Resolution;

///<summary>Creates instances of <see cref="IScope"/>></summary>
public interface IScopeFactory
{
   IScope BeginScope();
}
