using Compze.Tessaging.Implementation.MessageHandling.Abstractions;

namespace Compze.Tessaging.Implementation.MessageHandling.Dispatching;

interface IMessageDispatchingRule
{
   bool CanBeDispatched(IExecutingMessagesSnapshot executing, TransportMessage.InComing candidateMessage);
}
