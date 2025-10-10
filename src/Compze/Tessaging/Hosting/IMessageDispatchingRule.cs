using Compze.Tessaging.Hosting.Implementation;
using Compze.Tessaging.Hosting.Implementation.Abstractions;

namespace Compze.Tessaging.Hosting;

interface IMessageDispatchingRule
{
    bool CanBeDispatched(IExecutingMessagesSnapshot executing, TransportMessage.InComing candidateMessage);
}