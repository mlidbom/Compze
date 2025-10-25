using System;
using System.Threading.Tasks;
using Compze.Abstractions.Tessaging.Public;
using Compze.Tessaging.Implementation.Abstractions;

namespace Compze.Tessaging.Implementation.Transport.Client.Abstractions;

interface IInboxConnection : IDisposable
{
    TessageTypesInternal.EndpointInformation EndpointInformation { get; }
    Task SendAsync(IExactlyOnceTevent tevent);
    Task SendAsync(IExactlyOnceTommand tommand);

    Task PostAsync(IAtMostOnceHypermediaTommand tommand);
    Task<TTommandResult> PostAsync<TTommandResult>(IAtMostOnceTommand<TTommandResult> tommand);
    Task<TTueryResult> GetAsync<TTueryResult>(IRemotableTuery<TTueryResult> tuery);
}