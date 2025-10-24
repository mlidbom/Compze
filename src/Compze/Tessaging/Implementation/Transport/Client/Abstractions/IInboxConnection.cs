using System;
using System.Threading.Tasks;
using Compze.Abstractions.Tessaging.Public;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Implementation.Abstractions;

namespace Compze.Tessaging.Implementation.Transport.Client.Abstractions;

interface IInboxConnection : IDisposable
{
    MessageTypesInternal.EndpointInformation EndpointInformation { get; }
    Task SendAsync(IExactlyOnceEvent @event);
    Task SendAsync(IExactlyOnceCommand command);

    Task PostAsync(IAtMostOnceHypermediaCommand command);
    Task<TCommandResult> PostAsync<TCommandResult>(IAtMostOnceCommand<TCommandResult> command);
    Task<TQueryResult> GetAsync<TQueryResult>(IRemotableQuery<TQueryResult> query);
}