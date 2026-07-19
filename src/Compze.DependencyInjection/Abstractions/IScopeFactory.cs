namespace Compze.DependencyInjection.Abstractions;

///<summary>Creates instances of <see cref="IScope"/>></summary>
public interface IScopeFactory
{
   IScope BeginScope();
}
