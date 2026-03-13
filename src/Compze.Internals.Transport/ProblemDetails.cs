using System.Net.Http.Json;
using Compze.Contracts;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using JetBrains.Annotations;

namespace Compze.Internals.Transport;

#pragma warning disable CA1812 // Instantiated via JSON deserialization
[UsedImplicitly] public class ProblemDetails
{
   public string Type { get; init; } = "";
   public string Title { get; init; } = "";
   public int Status { get; init; }
   public string Detail { get; init; } = "";
   public string Instance { get; init; } = "";

   public static async Task<ProblemDetails> FromResponse(HttpResponseMessage response)
   {
      try
      {
         return (await response.Content.ReadFromJsonAsync<ProblemDetails>().caf())._assert().NotNull();
      }
      catch(Exception exception)
      {
         throw new FailedToExtractProblemDetailsException(response, exception);
      }
   }
}

class FailedToExtractProblemDetailsException(HttpResponseMessage response, Exception? innerException = null) : Exception($"""
                                                                                                                          Failed to extract problem details from response.
                                                                                                                          RequestUri: {response.RequestMessage?.RequestUri} 
                                                                                                                          Status code: {response.StatusCode}
                                                                                                                          Reason: {response.ReasonPhrase}
                                                                                                                          """, innerException);
