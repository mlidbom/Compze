using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;

namespace Compze.DependencyInjection.Specifications.CreatedBy_factories;

public class When_a_component_is_registered_with_a_CreatedBy_factory_of_each_dependency_count
{
   class Dependency1;
   class Dependency2;
   class Dependency3;
   class Dependency4;
   class Dependency5;
   class Dependency6;
   class Dependency7;
   class Dependency8;
   class Dependency9;
   class Dependency10;
   class Dependency11;
   class Dependency12;
   class Dependency13;
   class Dependency14;
   class Dependency15;
   class Dependency16;

   ///<summary>The component every arity registers: it records the dependency instances its factory received, so the
   /// specification can prove each <c>CreatedBy</c> overload resolved the right dependencies in the right order.</summary>
   class ComponentRecordingReceivedDependencies(params object[] receivedDependencies)
   {
      public object[] ReceivedDependencies { get; } = receivedDependencies;
   }

   class ComponentWith0Dependencies() : ComponentRecordingReceivedDependencies();
   class ComponentWith1Dependencies(Dependency1 dependency1) : ComponentRecordingReceivedDependencies(dependency1);
   class ComponentWith2Dependencies(Dependency1 dependency1, Dependency2 dependency2) : ComponentRecordingReceivedDependencies(dependency1, dependency2);
   class ComponentWith3Dependencies(Dependency1 dependency1, Dependency2 dependency2, Dependency3 dependency3) : ComponentRecordingReceivedDependencies(dependency1, dependency2, dependency3);
   class ComponentWith4Dependencies(Dependency1 dependency1, Dependency2 dependency2, Dependency3 dependency3, Dependency4 dependency4) : ComponentRecordingReceivedDependencies(dependency1, dependency2, dependency3, dependency4);
   class ComponentWith5Dependencies(Dependency1 dependency1, Dependency2 dependency2, Dependency3 dependency3, Dependency4 dependency4, Dependency5 dependency5) : ComponentRecordingReceivedDependencies(dependency1, dependency2, dependency3, dependency4, dependency5);
   class ComponentWith6Dependencies(Dependency1 dependency1, Dependency2 dependency2, Dependency3 dependency3, Dependency4 dependency4, Dependency5 dependency5, Dependency6 dependency6) : ComponentRecordingReceivedDependencies(dependency1, dependency2, dependency3, dependency4, dependency5, dependency6);
   class ComponentWith7Dependencies(Dependency1 dependency1, Dependency2 dependency2, Dependency3 dependency3, Dependency4 dependency4, Dependency5 dependency5, Dependency6 dependency6, Dependency7 dependency7) : ComponentRecordingReceivedDependencies(dependency1, dependency2, dependency3, dependency4, dependency5, dependency6, dependency7);
   class ComponentWith8Dependencies(Dependency1 dependency1, Dependency2 dependency2, Dependency3 dependency3, Dependency4 dependency4, Dependency5 dependency5, Dependency6 dependency6, Dependency7 dependency7, Dependency8 dependency8) : ComponentRecordingReceivedDependencies(dependency1, dependency2, dependency3, dependency4, dependency5, dependency6, dependency7, dependency8);
   class ComponentWith9Dependencies(Dependency1 dependency1, Dependency2 dependency2, Dependency3 dependency3, Dependency4 dependency4, Dependency5 dependency5, Dependency6 dependency6, Dependency7 dependency7, Dependency8 dependency8, Dependency9 dependency9) : ComponentRecordingReceivedDependencies(dependency1, dependency2, dependency3, dependency4, dependency5, dependency6, dependency7, dependency8, dependency9);
   class ComponentWith10Dependencies(Dependency1 dependency1, Dependency2 dependency2, Dependency3 dependency3, Dependency4 dependency4, Dependency5 dependency5, Dependency6 dependency6, Dependency7 dependency7, Dependency8 dependency8, Dependency9 dependency9, Dependency10 dependency10) : ComponentRecordingReceivedDependencies(dependency1, dependency2, dependency3, dependency4, dependency5, dependency6, dependency7, dependency8, dependency9, dependency10);
   class ComponentWith11Dependencies(Dependency1 dependency1, Dependency2 dependency2, Dependency3 dependency3, Dependency4 dependency4, Dependency5 dependency5, Dependency6 dependency6, Dependency7 dependency7, Dependency8 dependency8, Dependency9 dependency9, Dependency10 dependency10, Dependency11 dependency11) : ComponentRecordingReceivedDependencies(dependency1, dependency2, dependency3, dependency4, dependency5, dependency6, dependency7, dependency8, dependency9, dependency10, dependency11);
   class ComponentWith12Dependencies(Dependency1 dependency1, Dependency2 dependency2, Dependency3 dependency3, Dependency4 dependency4, Dependency5 dependency5, Dependency6 dependency6, Dependency7 dependency7, Dependency8 dependency8, Dependency9 dependency9, Dependency10 dependency10, Dependency11 dependency11, Dependency12 dependency12) : ComponentRecordingReceivedDependencies(dependency1, dependency2, dependency3, dependency4, dependency5, dependency6, dependency7, dependency8, dependency9, dependency10, dependency11, dependency12);
   class ComponentWith13Dependencies(Dependency1 dependency1, Dependency2 dependency2, Dependency3 dependency3, Dependency4 dependency4, Dependency5 dependency5, Dependency6 dependency6, Dependency7 dependency7, Dependency8 dependency8, Dependency9 dependency9, Dependency10 dependency10, Dependency11 dependency11, Dependency12 dependency12, Dependency13 dependency13) : ComponentRecordingReceivedDependencies(dependency1, dependency2, dependency3, dependency4, dependency5, dependency6, dependency7, dependency8, dependency9, dependency10, dependency11, dependency12, dependency13);
   class ComponentWith14Dependencies(Dependency1 dependency1, Dependency2 dependency2, Dependency3 dependency3, Dependency4 dependency4, Dependency5 dependency5, Dependency6 dependency6, Dependency7 dependency7, Dependency8 dependency8, Dependency9 dependency9, Dependency10 dependency10, Dependency11 dependency11, Dependency12 dependency12, Dependency13 dependency13, Dependency14 dependency14) : ComponentRecordingReceivedDependencies(dependency1, dependency2, dependency3, dependency4, dependency5, dependency6, dependency7, dependency8, dependency9, dependency10, dependency11, dependency12, dependency13, dependency14);
   class ComponentWith15Dependencies(Dependency1 dependency1, Dependency2 dependency2, Dependency3 dependency3, Dependency4 dependency4, Dependency5 dependency5, Dependency6 dependency6, Dependency7 dependency7, Dependency8 dependency8, Dependency9 dependency9, Dependency10 dependency10, Dependency11 dependency11, Dependency12 dependency12, Dependency13 dependency13, Dependency14 dependency14, Dependency15 dependency15) : ComponentRecordingReceivedDependencies(dependency1, dependency2, dependency3, dependency4, dependency5, dependency6, dependency7, dependency8, dependency9, dependency10, dependency11, dependency12, dependency13, dependency14, dependency15);
   class ComponentWith16Dependencies(Dependency1 dependency1, Dependency2 dependency2, Dependency3 dependency3, Dependency4 dependency4, Dependency5 dependency5, Dependency6 dependency6, Dependency7 dependency7, Dependency8 dependency8, Dependency9 dependency9, Dependency10 dependency10, Dependency11 dependency11, Dependency12 dependency12, Dependency13 dependency13, Dependency14 dependency14, Dependency15 dependency15, Dependency16 dependency16) : ComponentRecordingReceivedDependencies(dependency1, dependency2, dependency3, dependency4, dependency5, dependency6, dependency7, dependency8, dependency9, dependency10, dependency11, dependency12, dependency13, dependency14, dependency15, dependency16);

   [DependencyInjectionContainerMatrix]
   public void each_factory_receives_the_registered_singleton_dependencies_in_declaration_order()
   {
      var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      builder.Registrar.Register(
         Singleton.For<Dependency1>().CreatedBy(() => new Dependency1()),
         Singleton.For<Dependency2>().CreatedBy(() => new Dependency2()),
         Singleton.For<Dependency3>().CreatedBy(() => new Dependency3()),
         Singleton.For<Dependency4>().CreatedBy(() => new Dependency4()),
         Singleton.For<Dependency5>().CreatedBy(() => new Dependency5()),
         Singleton.For<Dependency6>().CreatedBy(() => new Dependency6()),
         Singleton.For<Dependency7>().CreatedBy(() => new Dependency7()),
         Singleton.For<Dependency8>().CreatedBy(() => new Dependency8()),
         Singleton.For<Dependency9>().CreatedBy(() => new Dependency9()),
         Singleton.For<Dependency10>().CreatedBy(() => new Dependency10()),
         Singleton.For<Dependency11>().CreatedBy(() => new Dependency11()),
         Singleton.For<Dependency12>().CreatedBy(() => new Dependency12()),
         Singleton.For<Dependency13>().CreatedBy(() => new Dependency13()),
         Singleton.For<Dependency14>().CreatedBy(() => new Dependency14()),
         Singleton.For<Dependency15>().CreatedBy(() => new Dependency15()),
         Singleton.For<Dependency16>().CreatedBy(() => new Dependency16()),
         Singleton.For<ComponentWith0Dependencies>().CreatedBy(() => new ComponentWith0Dependencies()),
         Singleton.For<ComponentWith1Dependencies>().CreatedBy((Dependency1 dependency1) => new ComponentWith1Dependencies(dependency1)),
         Singleton.For<ComponentWith2Dependencies>().CreatedBy((Dependency1 dependency1, Dependency2 dependency2) => new ComponentWith2Dependencies(dependency1, dependency2)),
         Singleton.For<ComponentWith3Dependencies>().CreatedBy((Dependency1 dependency1, Dependency2 dependency2, Dependency3 dependency3) => new ComponentWith3Dependencies(dependency1, dependency2, dependency3)),
         Singleton.For<ComponentWith4Dependencies>().CreatedBy((Dependency1 dependency1, Dependency2 dependency2, Dependency3 dependency3, Dependency4 dependency4) => new ComponentWith4Dependencies(dependency1, dependency2, dependency3, dependency4)),
         Singleton.For<ComponentWith5Dependencies>().CreatedBy((Dependency1 dependency1, Dependency2 dependency2, Dependency3 dependency3, Dependency4 dependency4, Dependency5 dependency5) => new ComponentWith5Dependencies(dependency1, dependency2, dependency3, dependency4, dependency5)),
         Singleton.For<ComponentWith6Dependencies>().CreatedBy((Dependency1 dependency1, Dependency2 dependency2, Dependency3 dependency3, Dependency4 dependency4, Dependency5 dependency5, Dependency6 dependency6) => new ComponentWith6Dependencies(dependency1, dependency2, dependency3, dependency4, dependency5, dependency6)),
         Singleton.For<ComponentWith7Dependencies>().CreatedBy((Dependency1 dependency1, Dependency2 dependency2, Dependency3 dependency3, Dependency4 dependency4, Dependency5 dependency5, Dependency6 dependency6, Dependency7 dependency7) => new ComponentWith7Dependencies(dependency1, dependency2, dependency3, dependency4, dependency5, dependency6, dependency7)),
         Singleton.For<ComponentWith8Dependencies>().CreatedBy((Dependency1 dependency1, Dependency2 dependency2, Dependency3 dependency3, Dependency4 dependency4, Dependency5 dependency5, Dependency6 dependency6, Dependency7 dependency7, Dependency8 dependency8) => new ComponentWith8Dependencies(dependency1, dependency2, dependency3, dependency4, dependency5, dependency6, dependency7, dependency8)),
         Singleton.For<ComponentWith9Dependencies>().CreatedBy((Dependency1 dependency1, Dependency2 dependency2, Dependency3 dependency3, Dependency4 dependency4, Dependency5 dependency5, Dependency6 dependency6, Dependency7 dependency7, Dependency8 dependency8, Dependency9 dependency9) => new ComponentWith9Dependencies(dependency1, dependency2, dependency3, dependency4, dependency5, dependency6, dependency7, dependency8, dependency9)),
         Singleton.For<ComponentWith10Dependencies>().CreatedBy((Dependency1 dependency1, Dependency2 dependency2, Dependency3 dependency3, Dependency4 dependency4, Dependency5 dependency5, Dependency6 dependency6, Dependency7 dependency7, Dependency8 dependency8, Dependency9 dependency9, Dependency10 dependency10) => new ComponentWith10Dependencies(dependency1, dependency2, dependency3, dependency4, dependency5, dependency6, dependency7, dependency8, dependency9, dependency10)),
         Singleton.For<ComponentWith11Dependencies>().CreatedBy((Dependency1 dependency1, Dependency2 dependency2, Dependency3 dependency3, Dependency4 dependency4, Dependency5 dependency5, Dependency6 dependency6, Dependency7 dependency7, Dependency8 dependency8, Dependency9 dependency9, Dependency10 dependency10, Dependency11 dependency11) => new ComponentWith11Dependencies(dependency1, dependency2, dependency3, dependency4, dependency5, dependency6, dependency7, dependency8, dependency9, dependency10, dependency11)),
         Singleton.For<ComponentWith12Dependencies>().CreatedBy((Dependency1 dependency1, Dependency2 dependency2, Dependency3 dependency3, Dependency4 dependency4, Dependency5 dependency5, Dependency6 dependency6, Dependency7 dependency7, Dependency8 dependency8, Dependency9 dependency9, Dependency10 dependency10, Dependency11 dependency11, Dependency12 dependency12) => new ComponentWith12Dependencies(dependency1, dependency2, dependency3, dependency4, dependency5, dependency6, dependency7, dependency8, dependency9, dependency10, dependency11, dependency12)),
         Singleton.For<ComponentWith13Dependencies>().CreatedBy((Dependency1 dependency1, Dependency2 dependency2, Dependency3 dependency3, Dependency4 dependency4, Dependency5 dependency5, Dependency6 dependency6, Dependency7 dependency7, Dependency8 dependency8, Dependency9 dependency9, Dependency10 dependency10, Dependency11 dependency11, Dependency12 dependency12, Dependency13 dependency13) => new ComponentWith13Dependencies(dependency1, dependency2, dependency3, dependency4, dependency5, dependency6, dependency7, dependency8, dependency9, dependency10, dependency11, dependency12, dependency13)),
         Singleton.For<ComponentWith14Dependencies>().CreatedBy((Dependency1 dependency1, Dependency2 dependency2, Dependency3 dependency3, Dependency4 dependency4, Dependency5 dependency5, Dependency6 dependency6, Dependency7 dependency7, Dependency8 dependency8, Dependency9 dependency9, Dependency10 dependency10, Dependency11 dependency11, Dependency12 dependency12, Dependency13 dependency13, Dependency14 dependency14) => new ComponentWith14Dependencies(dependency1, dependency2, dependency3, dependency4, dependency5, dependency6, dependency7, dependency8, dependency9, dependency10, dependency11, dependency12, dependency13, dependency14)),
         Singleton.For<ComponentWith15Dependencies>().CreatedBy((Dependency1 dependency1, Dependency2 dependency2, Dependency3 dependency3, Dependency4 dependency4, Dependency5 dependency5, Dependency6 dependency6, Dependency7 dependency7, Dependency8 dependency8, Dependency9 dependency9, Dependency10 dependency10, Dependency11 dependency11, Dependency12 dependency12, Dependency13 dependency13, Dependency14 dependency14, Dependency15 dependency15) => new ComponentWith15Dependencies(dependency1, dependency2, dependency3, dependency4, dependency5, dependency6, dependency7, dependency8, dependency9, dependency10, dependency11, dependency12, dependency13, dependency14, dependency15)),
         Singleton.For<ComponentWith16Dependencies>().CreatedBy((Dependency1 dependency1, Dependency2 dependency2, Dependency3 dependency3, Dependency4 dependency4, Dependency5 dependency5, Dependency6 dependency6, Dependency7 dependency7, Dependency8 dependency8, Dependency9 dependency9, Dependency10 dependency10, Dependency11 dependency11, Dependency12 dependency12, Dependency13 dependency13, Dependency14 dependency14, Dependency15 dependency15, Dependency16 dependency16) => new ComponentWith16Dependencies(dependency1, dependency2, dependency3, dependency4, dependency5, dependency6, dependency7, dependency8, dependency9, dependency10, dependency11, dependency12, dependency13, dependency14, dependency15, dependency16))
      );

      using var container = builder.Build();

      object[] dependenciesInDeclarationOrder = [container.Resolve<Dependency1>(), container.Resolve<Dependency2>(), container.Resolve<Dependency3>(), container.Resolve<Dependency4>(), container.Resolve<Dependency5>(), container.Resolve<Dependency6>(), container.Resolve<Dependency7>(), container.Resolve<Dependency8>(), container.Resolve<Dependency9>(), container.Resolve<Dependency10>(), container.Resolve<Dependency11>(), container.Resolve<Dependency12>(), container.Resolve<Dependency13>(), container.Resolve<Dependency14>(), container.Resolve<Dependency15>(), container.Resolve<Dependency16>()];

      ComponentRecordingReceivedDependencies[] componentsByDependencyCount =
      [
         container.Resolve<ComponentWith0Dependencies>(),
         container.Resolve<ComponentWith1Dependencies>(),
         container.Resolve<ComponentWith2Dependencies>(),
         container.Resolve<ComponentWith3Dependencies>(),
         container.Resolve<ComponentWith4Dependencies>(),
         container.Resolve<ComponentWith5Dependencies>(),
         container.Resolve<ComponentWith6Dependencies>(),
         container.Resolve<ComponentWith7Dependencies>(),
         container.Resolve<ComponentWith8Dependencies>(),
         container.Resolve<ComponentWith9Dependencies>(),
         container.Resolve<ComponentWith10Dependencies>(),
         container.Resolve<ComponentWith11Dependencies>(),
         container.Resolve<ComponentWith12Dependencies>(),
         container.Resolve<ComponentWith13Dependencies>(),
         container.Resolve<ComponentWith14Dependencies>(),
         container.Resolve<ComponentWith15Dependencies>(),
         container.Resolve<ComponentWith16Dependencies>()
      ];

      for(var dependencyCount = 0; dependencyCount <= 16; dependencyCount++)
      {
         componentsByDependencyCount[dependencyCount].ReceivedDependencies
            .Must().SequenceEqual(dependenciesInDeclarationOrder.Take(dependencyCount));
      }
   }
}
