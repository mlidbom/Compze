using Compze.Abstractions.Hosting.Public;
using Compze.TypeIdentifiers;
using Compze.Hosting.Configuration;
using Compze.Internals.Transport;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Hosting;

///<summary>
/// The <see cref="IEndpointBuilder"/> mechanism. It registers only what every endpoint needs regardless of
/// paradigm — the type mapper (pre-mapped with the shared message hierarchy and discovery types), the
/// endpoint's identity, the configuration provider, and the infrastructure-query executor — and collects what
/// features contribute: container registrations, <see cref="IEndpointComponent"/> factories, and
/// post-container-build actions. <see cref="Build"/> then assembles the <see cref="Endpoint"/>.
///</summary>
class ServerEndpointBuilder : IEndpointBuilder
{
   internal IContainerBuilder Builder { get; }
   public IComponentRegistrar Registrar => Builder.Registrar;
   public EndpointConfiguration Configuration { get; }

   readonly TypeMapper _typeMapper;
   public ITypeMapper TypeMapper => _typeMapper;
   public ITypeMap TypeMap => _typeMapper;

   readonly Dictionary<Type, object> _features = [];
   readonly List<Func<IRootResolver, IEndpointComponent>> _componentFactories = [];
   readonly List<Action<IRootResolver>> _containerBuiltActions = [];

   public ServerEndpointBuilder(IContainerBuilder builder, EndpointConfiguration configuration)
   {
      Builder = builder;
      Configuration = configuration;
      _typeMapper = new TypeMapper();
   }

   public TFeature GetOrAddFeature<TFeature>(Func<IEndpointBuilder, TFeature> createFeature) where TFeature : class
   {
      if(_features.TryGetValue(typeof(TFeature), out var existingFeature)) return (TFeature)existingFeature;
      var feature = createFeature(this);
      _features.Add(typeof(TFeature), feature);
      return feature;
   }

   public void AddComponent(Func<IRootResolver, IEndpointComponent> createComponent) => _componentFactories.Add(createComponent);

   public void OnContainerBuilt(Action<IRootResolver> action) => _containerBuiltActions.Add(action);

   public IEndpoint Build()
   {
      SetupContainer();
      var container = Builder.Build();
      var rootResolver = container.RootResolver;
      _containerBuiltActions.ForEach(action => action(rootResolver));
      return new Endpoint(container, Configuration, _componentFactories);
   }

   void SetupContainer()
   {
      _typeMapper.MapTypesFromAssemblyContaining<EndpointAddress>();          // Compze.Abstractions — the shared message-type hierarchy and hosting contracts
      _typeMapper.MapTypesFromAssemblyContaining<EndpointInformationQuery>(); // Compze.Internals.Transport — endpoint discovery infrastructure

      Registrar.JSonAppConfigFileConfigurationParameterProvider()
               .Register(Singleton.For<ITypeMapper>().Instance(_typeMapper),
                         Singleton.For<ITypeMap>().Instance(_typeMapper),
                         Singleton.For<EndpointId>().Instance(Configuration.Id),
                         Singleton.For<EndpointConfiguration>().Instance(Configuration));

      InfrastructureQueryExecutor.RegisterWith(Registrar);
   }
}
