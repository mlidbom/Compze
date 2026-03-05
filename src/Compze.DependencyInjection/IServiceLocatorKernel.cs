namespace Compze.DependencyInjection;

public interface IServiceLocatorKernel
{
   TComponent Resolve<TComponent>() where TComponent : class;
}
