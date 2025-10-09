# Double-Disposal Fix for Microsoft DI Integration

## The Problem

When using `RegisterToHandleServiceResolutionFor()` to integrate Compze's DI container with Microsoft's DI (ASP.NET Core), components were being disposed twice:

1. Once by Microsoft's `ServiceProvider.Dispose()`
2. Once by Compze's `IDependencyInjectionContainer.Dispose()`

This occurred because Microsoft DI tracks and disposes ALL services it resolves (even transient ones), which is different from most DI containers.

## The Solution: `CompzeServiceProviderFactory`

Instead of registering services into Microsoft's container directly, we now provide a custom `IServiceProviderFactory` that creates a hybrid service provider:

### How It Works

1. **ASP.NET Core adds its services** to the `IServiceCollection` normally
2. **`CompzeServiceProviderFactory.CreateServiceProvider`** builds Microsoft's provider for ASP.NET services
3. **`HybridServiceProvider`** wraps both providers:
   - Tries to resolve from Compze container first (for your services)
   - Falls back to Microsoft provider (for ASP.NET Core services)
4. **Only Microsoft's provider is disposed** (by ASP.NET Core)
5. **Compze's container is disposed separately** by your application

### Benefits

✅ No double-disposal - each service is disposed by only one container  
✅ ASP.NET Core services work normally  
✅ Scoped lifetimes work correctly  
✅ Your services remain under your container's control  

## Usage

### ASP.NET Core (Program.cs)

```csharp
var builder = WebApplication.CreateBuilder(args);

// Create your Compze container
var container = new MicrosoftDependencyInjectionContainer(RunMode.Production);

// Register your services
container.Register(
    Singleton.For<IMyService>().CreatedBy<MyService>(),
    Scoped.For<IMyScopedService>().CreatedBy<MyScopedService>()
);

// Use the custom factory
builder.Host.UseServiceProviderFactory(
    container.CreateServiceProviderFactory());

// ASP.NET Core services are registered normally
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
// etc.

var app = builder.Build();

// ... configure middleware ...

app.Run();

// Dispose your container when the app shuts down
container.Dispose();
```

### Important Notes

- **DO use** `CreateServiceProviderFactory()` for ASP.NET Core integration
- **DO NOT use** `RegisterToHandleServiceResolutionFor()` (it's marked obsolete)
- **DO dispose** your Compze container separately from the ASP.NET Core host
- **ASP.NET Core will dispose** its `ServiceProvider` automatically

## Tests

See `MicrosoftDependencyInjectionTransientDisposalTests.cs` for comprehensive tests demonstrating:
- Microsoft DI's disposal behavior
- Double-disposal scenarios (before fix)
- Proper disposal with the new factory (after fix)

## Technical Details

### HybridServiceProvider

The `HybridServiceProvider` implements:
- `IServiceProvider` - Standard service resolution
- `ISupportRequiredService` - Required service semantics
- `IServiceScopeFactory` - Scoped lifetime support
- `IDisposable` / `IAsyncDisposable` - Only disposes Microsoft's provider

### Scope Management

When creating a scope:
1. A scope is created in Compze's container
2. A scope is created in Microsoft's provider
3. Both are wrapped in `HybridServiceScope`
4. Disposing the scope disposes both underlying scopes

This ensures scoped services work correctly in both containers while avoiding double-disposal.
