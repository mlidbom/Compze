using Compze.Abstractions.Tessaging.Public;

namespace Compze.Typermedia;

public interface ITypermediaRouting
{
   Task PostAsync(IAtMostOnceTypermediaTommand tommand);
   Task<TTommandResult> PostAsync<TTommandResult>(IAtMostOnceTommand<TTommandResult> typermediaTommand);
   Task<TTueryResult> GetAsync<TTueryResult>(IRemotableTuery<TTueryResult> tuery);
}
