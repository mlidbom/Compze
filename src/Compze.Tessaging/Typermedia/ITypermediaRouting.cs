using Compze.Tessaging.Abstractions.TessageTypes;

namespace Compze.Tessaging.Typermedia;

interface ITypermediaRouting
{
   Task PostAsync(IAtMostOnceTypermediaTommand tommand);
   Task<TTommandResult> PostAsync<TTommandResult>(IAtMostOnceTypermediaTommand<TTommandResult> typermediaTommand);
   Task<TTueryResult> GetAsync<TTueryResult>(IRemotableTuery<TTueryResult> tuery);
}
