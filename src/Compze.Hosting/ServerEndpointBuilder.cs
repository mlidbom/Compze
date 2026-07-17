using Compze.Abstractions.Configuration.Internal;
using Compze.Abstractions.Hosting.Public;
using Compze.TypeIdentifiers;
using Compze.Hosting.Configuration;
using Compze.Tessaging.Internals.Transport;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Hosting;

///<summary>
/// The <see cref="IEndpointBuilder"/> mechanism. It registers only what every endpoint needs no matter what
/// it speaks — the type mapper (pre-mapped with the shared message hierarchy and discovery types), the
/// endpoint's identity, the configuration provider, and the endpoint-discovery query executor — and collects what
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
      _typeMapper.MapTypesFromAssemblyContaining<EndpointAddress>(); // Compze.Abstractions — the shared message-type hierarchy and hosting contracts

      //Guarded: a composition that supplies its own configuration source registers its IConfigurationParameterProvider in the endpoint setup; appsettings.json is only the default.
      if(!Registrar.IsRegistered<IConfigurationParameterProvider>())
         Registrar.JSonAppConfigFileConfigurationParameterProvider();

      Registrar.Register(Singleton.For<ITypeMapper>().Instance(_typeMapper),
                         Singleton.For<ITypeMap>().Instance(_typeMapper),
                         Singleton.For<EndpointId>().Instance(Configuration.Id),
                         Singleton.For<EndpointConfiguration>().Instance(Configuration));

      EndpointDiscoveryQueryExecutor.RegisterWith(Registrar);
   }
}
