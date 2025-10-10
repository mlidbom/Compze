namespace Compze.Utilities.DependencyInjection;

interface IServiceLocatorKernel
{
   TComponent Resolve<TComponent>() where TComponent : class;
}
