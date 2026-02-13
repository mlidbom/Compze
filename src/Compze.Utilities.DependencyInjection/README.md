# Compze.Utilities.DependencyInjection

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

### Core abstractions

- **`IDependencyInjectionContainer`** — Container lifecycle, registration, and `IServiceLocator` access
- **`IServiceLocator`** — Resolve services by type, create scoped locators
- **`IComponentRegistrar`** — Register components with lifestyle and factory methods
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
dotnet add package Compze.Utilities.DependencyInjection
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Utilities.DependencyInjection.Microsoft](https://www.nuget.org/packages/Compze.Utilities.DependencyInjection.Microsoft) | Microsoft DI integration |
| [Compze.Utilities.DependencyInjection.SimpleInjector](https://www.nuget.org/packages/Compze.Utilities.DependencyInjection.SimpleInjector) | SimpleInjector integration |
| [Compze.Utilities](https://www.nuget.org/packages/Compze.Utilities) | Core utilities |

## License

Apache-2.0
