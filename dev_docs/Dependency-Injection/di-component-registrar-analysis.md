# IComponentRegistrar — Role & Design Analysis

## What It Is

`IComponentRegistrar` is a strategy object that encapsulates registration policy. It provides the fluent registration API and enables clean separation between production and testing wiring.

## Interface

```csharp
interface IComponentRegistrar
{
   IComponentRegistrar Register(params ComponentRegistration[] registrations);
   IComponentRegistrar Register(params Action<IComponentRegistrar>[] registrationMethods);  // default impl
   bool IsClone { get; }
   bool IsRegistered<TComponent>() where TComponent : class;
   TTestingRegistrar? TryGetTestingRegistrar<TTestingRegistrar>() where TTestingRegistrar : class;
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

71 extension methods across subsystems follow this pattern. Each extends `IComponentRegistrar`, calls `registrar.Register(...)` internally, returns the registrar.

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

### 4. Builder reference & clone support

- The concrete `ComponentRegistrar` holds a `ContainerBuilderBase` reference set via internal `SetBuilder()`. This is how `Register()` calls flow through to the builder.
- `IsClone` — delegates to `_builder.IsClone`
- `IsRegistered<T>()` — queries the builder's `RegisteredComponents()` list
- `Clone()` — creates an empty registrar instance for container cloning. `TestingComponentRegistrar` overrides to return its own subtype.

## Registration Flow

```
Extension method call (fluent API)
  → static RegisterWith() (internal wiring)
    → registrar.Register(ComponentRegistration[])
      → _builder.Register(ComponentRegistration[])     // ContainerBuilderBase
        → _registeredComponents.Add(...)
        → RegisterInContainer(...)                     // abstract, per DI backend
          → Autofac/Microsoft DI actual registration
```

## Relationship to IContainerBuilder (implemented)

`IContainerBuilder` composes the registrar — it doesn't replace it:
- `IContainerBuilder.Registrar` exposes `IComponentRegistrar` for wiring code
- 71 extension methods target `IComponentRegistrar`, not the builder
- The testing strategy polymorphism is on the registrar, not the builder
- `Clone()` on the registrar is used internally by `DependencyInjectionContainer.Clone()`
- Wiring code takes `IComponentRegistrar` as parameter — it doesn't need `Build()` or any builder-level concept

The registrar is a **registration-phase concern** that the builder owns.

## Resolved Design Questions

- `Container()` was removed from the interface. The registrar now holds a `ContainerBuilderBase` reference instead (internal `SetBuilder()` on the concrete class). Wiring code doesn't need to reach the builder from the interface.
- `SetContainer()` was replaced by `SetBuilder()` on the concrete `ComponentRegistrar`. Still a mutable init pattern — the builder sets itself on the registrar during construction.
- The `Action<IComponentRegistrar>[]` overload remains — wiring code targets `IComponentRegistrar`, not the builder.
