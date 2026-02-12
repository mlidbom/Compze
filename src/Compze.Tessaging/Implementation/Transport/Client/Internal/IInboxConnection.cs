using System;
using System.Threading.Tasks;
using Compze.Core.Tessaging.Public;
using Compze.Tessaging.Implementation.Abstractions;

namespace Compze.Tessaging.Implementation.Transport.Client.Internal;

interface IInboxConnection : IDisposable
{
    TessageTypesInternal.EndpointInformation EndpointInformation { get; }
    Task SendAsync(IExactlyOnceTevent tevent);
    Task SendAsync(IExactlyOnceTommand tommand);

    Task PostAsync(IAtMostOnceTypermediaTommand tommand);
    Task<TTommandResult> PostAsync<TTommandResult>(IAtMostOnceTommand<TTommandResult> typermediaTommand);
    Task<TTueryResult> GetAsync<TTueryResult>(IRemotableTuery<TTueryResult> tuery);
}