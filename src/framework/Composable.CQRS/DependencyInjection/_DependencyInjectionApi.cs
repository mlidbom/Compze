﻿using System;
using System.Collections.Generic;

namespace Composable.DependencyInjection;

public interface IDependencyInjectionContainer : IDisposable, IAsyncDisposable
{
   IRunMode RunMode { get; }
   void Register(params ComponentRegistration[] registrations);
   IEnumerable<ComponentRegistration> RegisteredComponents();
   IServiceLocator CreateServiceLocator();
}

public interface IServiceLocator : IDisposable, IAsyncDisposable
{
   TComponent Resolve<TComponent>() where TComponent : class;
   TComponent[] ResolveAll<TComponent>() where TComponent : class;
   IDisposable BeginScope();
}

interface IServiceLocatorKernel
{
   TComponent Resolve<TComponent>() where TComponent : class;
}

public interface IRunMode
{
   bool IsTesting { get; }
}

public enum PersistenceLayer
{
   MicrosoftSQLServer,
   Memory,
   MySql,
   PostgreSql,
   Oracle,
   IBMDB2
}

public enum DIContainer
{
   Composable, SimpleInjector, WindsorCastle, Microsoft
}

enum Lifestyle
{
   Singleton,
   Scoped
}