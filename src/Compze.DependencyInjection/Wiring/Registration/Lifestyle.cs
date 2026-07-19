using Compze.DependencyInjection.Runtime.Resolution;

namespace Compze.DependencyInjection.Wiring.Registration;

///<summary>The supported service lifestyles</summary>
public enum Lifestyle
{
   ///<summary>Every call to <see cref="IServiceResolver.Resolve"/> will return the same instance for a service registered as <see cref="Singleton"/></summary>
   Singleton,

   ///<summary>
   /// <see cref="Scoped"/> services can only be resolved within an <see cref="IScope"/>, preferably through an <see cref="IScopeResolver"/>.
   /// <para>While inside a scope, every call to <see cref="IServiceResolver.Resolve"/> will return the same instance.</para>
   /// <para>Once the scope is disposed, all the <see cref="Scoped"/> instances are also disposed.</para>
   /// </summary>
   Scoped,

   ///<summary>
   /// Every call to <see cref="IServiceResolver.Resolve"/> will return a new unique instance of the service for a service registered as <see cref="TrackedTransient"/>.
   ///<para>If resolved within a scope, the instance will be disposed when the scope is disposed.</para>
   ///<para>If resolved outside a scope, the instance will be disposed when the container is disposed.</para>
   /// </summary>
   TrackedTransient
}
