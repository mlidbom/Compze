﻿using System;
using System.Linq;
using Castle.Core.Internal;
using Castle.MicroKernel;
using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.Windsor;

namespace Composable.DependencyInjection.Windsor
{
    class WindsorDependencyInjectionContainer : IDependencyInjectionContainer, IServiceLocator
    {
        readonly IWindsorContainer _windsorContainer;
        internal WindsorDependencyInjectionContainer()
        {
            _windsorContainer = new WindsorContainer();
            _windsorContainer.Kernel.Resolver.AddSubResolver(new CollectionResolver(_windsorContainer.Kernel));
        }

        public void Register(params CComponentRegistration[] registration)
        {
            var windsorRegistrations = registration.Select(ToWindsorRegistration)
                                                   .ToArray();

            _windsorContainer.Register(windsorRegistrations);
        }

        IServiceLocator IDependencyInjectionContainer.CreateServiceLocator() => this;

        bool IDependencyInjectionContainer.IsTestMode => _windsorContainer.Kernel.HasComponent(typeof(TestModeMarker));

        IComponentLease<TComponent> IServiceLocator.Lease<TComponent>(string componentName) => new WindsorComponentLease<TComponent>(_windsorContainer.Resolve<TComponent>(componentName), _windsorContainer.Kernel);
        IComponentLease<TComponent> IServiceLocator.Lease<TComponent>() => new WindsorComponentLease<TComponent>(_windsorContainer.Resolve<TComponent>(), _windsorContainer.Kernel);
        IMultiComponentLease<TComponent> IServiceLocator.LeaseAll<TComponent>() => new WindsorMultiComponentLease<TComponent>(_windsorContainer.ResolveAll<TComponent>().ToArray(), _windsorContainer.Kernel);
        IDisposable IServiceLocator.BeginScope() => _windsorContainer.BeginScope();
        void IDisposable.Dispose() => _windsorContainer.Dispose();

        IRegistration ToWindsorRegistration(CComponentRegistration componentRegistration)
        {
            ComponentRegistration<object> registration = Component.For(componentRegistration.ServiceTypes);

            if (componentRegistration.InstantiationSpec.Instance != null)
            {
                registration.Instance(componentRegistration.InstantiationSpec.Instance);
            }
            else if (componentRegistration.InstantiationSpec.ImplementationType != null)
            {
                registration.ImplementedBy(componentRegistration.InstantiationSpec.ImplementationType);
            }
            else if (componentRegistration.InstantiationSpec.FactoryMethod != null)
            {
                registration.UsingFactoryMethod(kernel => componentRegistration.InstantiationSpec.FactoryMethod(new WindsorServiceLocatorKernel(kernel)));
            }
            else
            {
                throw new Exception($"Invalid {nameof(InstantiationSpec)}");
            }


            if (!componentRegistration.Name.IsNullOrEmpty())
            {
                registration = registration.Named(componentRegistration.Name);
            }

            switch (componentRegistration.Lifestyle)
            {
                case Lifestyle.Singleton:
                    return registration.LifestyleSingleton();
                case Lifestyle.Scoped:
                    return registration.LifestyleScoped();
                default:
                    throw new ArgumentOutOfRangeException(nameof(componentRegistration.Lifestyle));
            }
        }

        sealed class WindsorServiceLocatorKernel : IServiceLocatorKernel
        {
            readonly IKernel _kernel;
            internal WindsorServiceLocatorKernel(IKernel kernel) => _kernel = kernel;

            TComponent IServiceLocatorKernel.Resolve<TComponent>() => _kernel.Resolve<TComponent>();
            TComponent IServiceLocatorKernel.Resolve<TComponent>(string componentName) => _kernel.Resolve<TComponent>(componentName);
        }

        sealed class WindsorComponentLease<T> : IComponentLease<T>
        {
            readonly IKernel _kernel;
            readonly T _instance;

            internal WindsorComponentLease(T component, IKernel kernel)
            {
                _kernel = kernel;
                _instance = component;
            }

            T IComponentLease<T>.Instance => _instance;
            void IDisposable.Dispose() => _kernel.ReleaseComponent(_instance);
        }

        sealed class WindsorMultiComponentLease<T> : IMultiComponentLease<T>
        {
            readonly IKernel _kernel;
            readonly T[] _instances;

            internal WindsorMultiComponentLease(T[] components, IKernel kernel)
            {
                _kernel = kernel;
                _instances = components;
            }

            T[] IMultiComponentLease<T>.Instances => _instances;
            void IDisposable.Dispose() => _instances.ForEach(instance => _kernel.ReleaseComponent(instance));
        }
    }
}