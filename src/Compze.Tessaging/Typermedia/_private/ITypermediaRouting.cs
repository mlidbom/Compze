
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.Typermedia._private;

interface ITypermediaRouting
{
   Task PostAsync(IAtMostOnceTypermediaTommand tommand);
   Task<TTommandResult> PostAsync<TTommandResult>(IAtMostOnceTypermediaTommand<TTommandResult> typermediaTommand);
   Task<TTueryResult> GetAsync<TTueryResult>(IRemotableTuery<TTueryResult> tuery);
}
