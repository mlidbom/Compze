
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.Typermedia.Private;

interface ITypermediaRouting
{
   Task PostAsync(IAtMostOnceTypermediaTommand tommand);
   Task<TTommandResult> PostAsync<TTommandResult>(IAtMostOnceTypermediaTommand<TTommandResult> typermediaTommand);
   Task<TTueryResult> GetAsync<TTueryResult>(IRemotableTuery<TTueryResult> tuery);
}
