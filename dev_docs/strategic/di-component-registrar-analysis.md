# IComponentRegistrar — Role & Design Analysis

## What It Is

`IComponentRegistrar` is a strategy object that encapsulates registration policy. It provides the fluent registration API and enables clean separation between production and testing wiring.

## Interface

```csharp
interface IComponentRegistrar
{
   IComponentRegistrar Register(params ComponentRegistration[] registrations);
   IComponentRegistrar Register(params Action<IComponentRegistrar>[] registrationMethods);  // default impl
   IDependencyInjectionContainer Container();
   TTestingRegistrar? TryGetTestingRegistrar<TTestingRegistrar>() where TTestingRegistrar : class;
   void SetContainer(IDependencyInjectionContainer container);
   IComponentRegistrar Clone();
}
```

## Core Responsibilities

### 1. Fluent registration API

All methods return `IComponentRegistrar` for chaining:
```csharp
container.Register()
   .CurrentTestsPluggableComponents(connectionStringName)
   .TeventStore(connectionStringName)
   .NewtonsoftSerializers()
   .HttpTypermediaTransport();
```

44+ extension methods across subsystems follow this pattern. Each extends `IComponentRegistrar`, calls `registrar.Register(...)` internally, returns the registrar.

### 2. Wiring delegation pattern

Two-layer API:
- **Extension method** (public, fluent) → **static `RegisterWith()`** (internal, does actual registration)

```csharp
// Public fluent API
public static IComponentRegistrar DbPool(this IComponentRegistrar registrar) =>
   Compze.DbPool.DbPool.RegisterWith(registrar, timeout);

// Internal registration
internal static IComponentRegistrar RegisterWith(IComponentRegistrar registrar, TimeSpan reservationLength) =>
   registrar.Register(Singleton.For<DbPool>()
                              .CreatedBy((IDbPoolSqlLayer sqlLayer) => new DbPool(sqlLayer, reservationLength))
                              .DelegateToParentServiceLocatorWhenCloning());
```

### 3. Testing strategy via subtype polymorphism

The registrar is a strategy object. Production code uses `ComponentRegistrar`. Tests use `TestingComponentRegistrar`. The test version knows about `TestEnv` and `DbPool` — production code never references testing infrastructure.

```csharp
// Production registrar
class ComponentRegistrar : IComponentRegistrar
{
   public virtual TTestingRegistrar? TryGetTestingRegistrar<TTestingRegistrar>() => null;
}

// Testing registrar
class TestingComponentRegistrar : ComponentRegistrar
{
   readonly IDictionary<Type, object> _testingRegistrars = new Dictionary<Type, object>()
   {
      { typeof(MsSqlSqlConnectionPoolRegistrar.ITestingRegistrar), new MsSqlDbPoolRegistrar(this) },
      { typeof(SqliteMemoryConnectionPoolRegistrar.ITestingRegistrar), new SqliteMemoryDbPoolRegistrar(this) },
      // ...
   };

   public override TTestingRegistrar? TryGetTestingRegistrar<TTestingRegistrar>()
      => _testingRegistrars.TryGetValue(typeof(TTestingRegistrar), out var value) ? (TTestingRegistrar)value : null;
}
```

Wiring code branches on the registrar type without referencing test frameworks:
```csharp
public static IComponentRegistrar SqliteMemoryConnectionPool(this IComponentRegistrar registrar, string name)
{
   if(registrar.TryGetTestingRegistrar<ITestingRegistrar>() is {} testingRegistrar)
      return testingRegistrar.Register(name);
   else
      throw new InvalidOperationException("SqliteMemory is only supported in testing mode");
}
```

Design comment from the code: *"Rather than passing an untyped IRunMode around and using it to make decisions, subtypes of IComponentRegistrar should be making the decisions."*

### 4. Container reference & clone support

- `Container()` — gives wiring code access to the container (used by the `RegisteredComponents()` query, `IsClone` check, etc.)
- `SetContainer()` — called during container construction to wire the registrar to its owning container
- `Clone()` — creates an empty registrar instance for container cloning. `TestingComponentRegistrar` overrides to return its own subtype.

## Registration Flow

```
Extension method call (fluent API)
  → static RegisterWith() (internal wiring)
    → registrar.Register(ComponentRegistration[])
      → container.Register(ComponentRegistration[])
        → _registeredComponents.Add(...)
        → RegisterInContainer(...)  // abstract, per DI backend
          → Autofac/Microsoft DI actual registration
```

## Relationship to IContainerBuilder

`IComponentRegistrar` currently does two things:
1. **Holds the fluent registration API** (extension methods, chaining)
2. **Encapsulates registration policy** (testing vs production via subtype)

In the new model, `IContainerBuilder` would need to compose the registrar (not replace it), because:
- The 44+ extension methods target `IComponentRegistrar`, not the container
- The testing strategy polymorphism is on the registrar, not the container
- `Clone()` is needed for container cloning / child container creation
- Wiring code takes `IComponentRegistrar` as parameter — it doesn't need `Build()` or any builder-level concept

The registrar is a **registration-phase concern** that the builder would own, just as the container currently owns it.

## Open Questions

- Should `Container()` exist on the registrar? It's a back-reference that couples the registrar to its owner. In the new model, would wiring code ever need to reach the builder from the registrar?
- The `Action<IComponentRegistrar>[]` overload enables passing `RegisterWith` methods as delegates. This is convenient but could also work as `Action<IContainerBuilder>` if the builder exposed registration.
- `SetContainer()` is a mutable init pattern. Could be replaced by constructor injection if the registrar is always created with its owner.
