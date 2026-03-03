using Compze.Core.Tessaging.Public;

namespace Compze.Core.Tessaging.Teventive.Infrastructure.Validation;

public static partial class TessageInspector
{
   public class AttemptToSendStrictlyLocalTessageRemotelyException(IStrictlyLocalTessage tessage) : Exception(RemoteSendOfStrictlyLocalTessageTessage(tessage))
   {
      static string RemoteSendOfStrictlyLocalTessageTessage(IStrictlyLocalTessage tessage) => $"""


                                                                                               {tessage.GetType().FullName} cannot be sent remotely because it implements {typeof(IStrictlyLocalTessage)}.

                                                                                               Rationale: 
                                                                                               {typeof(IStrictlyLocalTessage)} implementations are designed explicitly to be used locally.
                                                                                               The result of sending them off remotely is unclear to say the least and very unlikely to end up doing what you want. 

                                                                                               """;
   }
}