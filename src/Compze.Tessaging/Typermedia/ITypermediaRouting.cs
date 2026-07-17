using Compze.Abstractions.Tessaging.Public;

namespace Compze.Tessaging.Typermedia;

public interface ITypermediaRouting
{
   Task PostAsync(IAtMostOnceTypermediaTommand tommand);
   Task<TTommandResult> PostAsync<TTommandResult>(IAtMostOnceTypermediaTommand<TTommandResult> typermediaTommand);
   Task<TTueryResult> GetAsync<TTueryResult>(IRemotableTuery<TTueryResult> tuery);
}
