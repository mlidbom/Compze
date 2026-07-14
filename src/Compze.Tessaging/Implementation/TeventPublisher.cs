using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Validation;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Teventive.Tevents.Public;
using JetBrains.Annotations;

namespace Compze.Tessaging.Implementation;

static class TeventPublisherRegistrar
{
   public static IComponentRegistrar TeventPublisher(this IComponentRegistrar registrar)
      => registrar.Register(Implementation.TeventPublisher.RegisterWith);
}

///<summary>The <see cref="ITeventPublisher"/>: routes each published tevent by the delivery contract its type declares.<br/>
/// Participation — synchronous delivery to this process's handlers via <see cref="IInProcessTeventPublisher"/>, within the<br/>
/// caller's transaction — is the leg every tevent travels; an <see cref="IExactlyOnceTevent"/> additionally travels the<br/>
/// endpoint's <see cref="IExactlyOnceTeventDeliveryLeg"/> when the composition wires one. Zero wired legs is the deliberately<br/>
/// in-process composition, where participation already serves every subscriber that can exist.</summary>
[UsedImplicitly] class TeventPublisher : ITeventPublisher
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Scoped.For<ITeventPublisher>()
                                  .CreatedBy((IInProcessTeventPublisher inProcessTeventPublisher, IComponentSet<IExactlyOnceTeventDeliveryLeg> exactlyOnceDeliveryLegs, IScopeResolver scopeResolver)
                                                => new TeventPublisher(inProcessTeventPublisher, exactlyOnceDeliveryLegs, scopeResolver)));

   readonly IInProcessTeventPublisher _inProcessTeventPublisher;
   readonly IExactlyOnceTeventDeliveryLeg? _exactlyOnceDeliveryLeg;
   readonly IScopeResolver _scopeResolver;

   TeventPublisher(IInProcessTeventPublisher inProcessTeventPublisher, IEnumerable<IExactlyOnceTeventDeliveryLeg> exactlyOnceDeliveryLegs, IScopeResolver scopeResolver)
   {
      _inProcessTeventPublisher = inProcessTeventPublisher;
      _exactlyOnceDeliveryLeg = exactlyOnceDeliveryLegs.SingleOrDefault();
      _scopeResolver = scopeResolver;
   }

   public void Publish(ITevent tevent)
   {
      var wrappedTevent = PublisherIdentifyingTevent.Wrapped(tevent);
      if(wrappedTevent is IPublisherIdentifyingTevent<IExactlyOnceTevent> exactlyOnceTevent && _exactlyOnceDeliveryLeg != null)
      {
         TessageInspector.AssertValidToSendRemote(exactlyOnceTevent.Tevent);
         _inProcessTeventPublisher.Publish(wrappedTevent, _scopeResolver);
         _exactlyOnceDeliveryLeg.PublishTransactionally(exactlyOnceTevent);
      } else
      {
         _inProcessTeventPublisher.Publish(wrappedTevent, _scopeResolver);
      }
   }
}
