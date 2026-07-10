# Compze.DependencyInjection

Pluggable dependency injection abstractions for [Compze](https://github.com/mlidbom/Compze).

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

A container-agnostic dependency injection abstraction with a fluent registration API, lifestyle validation, scoped service resolution, and transactional scope execution.

### Registration API

```csharp
// Singleton with dependency injection
Singleton.For<IUserRepository>()
    .CreatedBy<IDbConnectionFactory>(factory => new UserRepository(factory));

// Scoped with multiple dependencies
Scoped.For<IOrderService>()
    .CreatedBy<IUserRepository, IEventBus>((repo, bus) => new OrderService(repo, bus));

// Pre-created singleton instance
Singleton.For<IConfiguration>()
    .Instance(myConfig);
```

### Breaking a circular dependency

When two components need each other, neither can be constructed first. Expose one side through an
`IServiceResolver<TService>` with `.WithServiceResolver()`, and have the other side depend on that resolver
instead of the service — it is constructed immediately holding only the resolver, and resolves the real
service later, on demand:

```csharp
Singleton.For<IServiceA>()
    .CreatedBy<ServiceA, IServiceResolver<IServiceB>>(serviceB => new ServiceA(serviceB));

Singleton.For<IServiceB>()
    .CreatedBy<ServiceB, IServiceA>(serviceA => new ServiceB(serviceA))
    .WithServiceResolver();

// class ServiceA(IServiceResolver<IServiceB> serviceB) : IServiceA
// {
//    // Resolve AFTER construction, never in the constructor — that would re-form the cycle.
//    public void DoWork() => serviceB.Resolve().Handle(this);
// }
```

A resolver is exposed for **each** service type the component is registered under (so a component registered
as `For<IServiceB, IServiceB2>()` is resolvable through both `IServiceResolver<IServiceB>` and
`IServiceResolver<IServiceB2>`). Each is registered at the target's own `Lifestyle`, so a dependency on
`IServiceResolver<TService>` is subject to exactly the same lifestyle validation as a direct dependency: a
`Singleton` still may not take an `IServiceResolver<TScoped>`.

`WithServiceResolver()` is not a core special case — it is an ordinary extension built on
`WithAssociatedRegistrations()`, the general mechanism by which a registration can carry extra registrations
that are added to the container alongside it. Consumers can write their own such helpers the same way.

### Core abstractions

- **`IDependencyInjectionContainer`** — Container lifecycle, registration, and `IServiceLocator` access
- **`IServiceLocator`** — Resolve services by type, create scoped locators
- **`IComponentRegistrar`** — Register components with lifestyle and factory methods
- **`IServiceResolver<TService>`** — Typed, deferred resolver for a single service; the supported way to break a constructor-injection cycle
- **`Lifestyle`** — `Singleton` or `Scoped`

### Safety features

- **Lifestyle validation** — Prevents singletons from depending on scoped components
- **Duplicate detection** — Catches double-registered service types
- **Container cloning** — Create isolated container copies for testing

### Transactional scope execution

```csharp
serviceLocator.ExecuteInIsolatedScope(locator =>
{
    var repo = locator.Resolve<IUserRepository>();
    repo.Save(user);
}); // Scope disposed, transaction committed
```

## Installation

```shell
dotnet add package Compze.DependencyInjection
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.DependencyInjection.Microsoft](https://www.nuget.org/packages/Compze.DependencyInjection.Microsoft) | Microsoft DI integration |
| [Compze.DependencyInjection.SimpleInjector](https://www.nuget.org/packages/Compze.DependencyInjection.SimpleInjector) | SimpleInjector integration |
| [Compze.Utilities](https://www.nuget.org/packages/Compze.Utilities) | Core utilities |

## License

Apache-2.0
