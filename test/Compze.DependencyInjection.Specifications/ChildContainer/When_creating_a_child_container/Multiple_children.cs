using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;


namespace Compze.DependencyInjection.Specifications.ChildContainer.When_creating_a_child_container;

public class Multiple_children
{
   [DependencyInjectionContainerMatrix]
   public void parent_can_create_multiple_children()
   {
      var parentBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      parentBuilder.Registrar.Register(Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService()));

      using var parent = parentBuilder.Build();
      using var child1 = parent.CreateChildContainerBuilder().Build();
      using var child2 = parent.CreateChildContainerBuilder().Build();

      var parentInstance = parent.Resolve<ISingletonService>();
      var child1Instance = child1.Resolve<ISingletonService>();
      var child2Instance = child2.Resolve<ISingletonService>();

      // All resolve to the same parent singleton
      child1Instance.Must().Be(parentInstance);
      child2Instance.Must().Be(parentInstance);
   }

   [DependencyInjectionContainerMatrix]
   public void children_have_independent_scoped_instances()
   {
      var parentBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      parentBuilder.Registrar.Register(Scoped.For<IScopedService>().CreatedBy(() => new ScopedService()));

      using var parent = parentBuilder.Build();
      using var child1 = parent.CreateChildContainerBuilder().Build();
      using var child2 = parent.CreateChildContainerBuilder().Build();

      IScopedService child1Instance;
      IScopedService child2Instance;

      {
         using var scope1 = child1.BeginScope();
         child1Instance = scope1.Resolve<IScopedService>();
      }

      {
         using var scope2 = child2.BeginScope();
         child2Instance = scope2.Resolve<IScopedService>();
      }

      child1Instance.Must().NotBe(child2Instance);
   }
}
