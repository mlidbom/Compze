using System;
using System.Net.Http;

namespace Composable.Messaging.Buses.Http;

class FailedToExtractProblemDetailsException : Exception
{
   public FailedToExtractProblemDetailsException(HttpResponseMessage response) : base($"""
                                                                                       Failed to extract problem details from response.
                                                                                       RequestUri: {response.RequestMessage?.RequestUri} 
                                                                                       Status code: {response.StatusCode}
                                                                                       Reason: {response.ReasonPhrase}
                                                                                       """)
   {
   }
}
