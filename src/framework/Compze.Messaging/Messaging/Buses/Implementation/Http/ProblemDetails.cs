﻿using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Compze.SystemCE;
using Compze.SystemCE.ThreadingCE.TasksCE;
using JetBrains.Annotations;

namespace Compze.Messaging.Buses.Implementation.Http;

[UsedImplicitly] class ProblemDetails
{
   public string Type { get; set; } = "";
   public string Title { get; set; } = "";
   public int Status { get; set; }
   public string Detail { get; set; } = "";
   public string Instance { get; set; } = "";

   internal static async Task<ProblemDetails> FromResponse(HttpResponseMessage response)
   {
      try
      {
         return (await response.Content.ReadFromJsonAsync<ProblemDetails>().CaF()).NotNull();
      }
      catch(Exception)
      {
         throw new FailedToExtractProblemDetailsException(response);
      }
   }
}

class FailedToExtractProblemDetailsException(HttpResponseMessage response) : Exception($"""
                                                                                        Failed to extract problem details from response.
                                                                                        RequestUri: {response.RequestMessage?.RequestUri} 
                                                                                        Status code: {response.StatusCode}
                                                                                        Reason: {response.ReasonPhrase}
                                                                                        """);
