using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Compze.Contracts;
using Compze.Utilities.SystemCE;
using Compze.Threading.TasksCE;
using JetBrains.Annotations;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Http;

#pragma warning disable CA1812 // Instantiated via JSON deserialization
[UsedImplicitly] class ProblemDetails
{
   public string Type { get; set; } = "";
   public string Title { get; set; } = "";
   public int Status { get; set; }
   public string Detail { get; set; } = "";
   public string Instance { get; set; } = "";

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

public class FailedToExtractProblemDetailsException(HttpResponseMessage response, Exception? innerException = null) : Exception($"""
                                                                                        Failed to extract problem details from response.
                                                                                        RequestUri: {response.RequestMessage?.RequestUri} 
                                                                                        Status code: {response.StatusCode}
                                                                                        Reason: {response.ReasonPhrase}
                                                                                        """, innerException);
