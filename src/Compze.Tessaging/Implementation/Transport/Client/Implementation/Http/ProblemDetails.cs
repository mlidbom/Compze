using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;
using JetBrains.Annotations;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Http;

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
         return (await response.Content.ReadFromJsonAsync<ProblemDetails>().caf()).NotNull();
      }
      catch(Exception)
      {
         throw new FailedToExtractProblemDetailsException(response);
      }
   }
}

public class FailedToExtractProblemDetailsException(HttpResponseMessage response) : Exception($"""
                                                                                        Failed to extract problem details from response.
                                                                                        RequestUri: {response.RequestMessage?.RequestUri} 
                                                                                        Status code: {response.StatusCode}
                                                                                        Reason: {response.ReasonPhrase}
                                                                                        """);
