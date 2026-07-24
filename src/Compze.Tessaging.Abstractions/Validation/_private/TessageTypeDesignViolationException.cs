namespace Compze.Tessaging.Validation._private;

internal class TessageTypeDesignViolationException(string violation) : Exception(violation + TypeDesignRationale)
{
   ///<summary>The violation alone, without <see cref="TypeDesignRationale"/> — for callers aggregating several violations into one report.</summary>
   public string Violation { get; } = violation;

   const string TypeDesignRationale = """


                                      Rationale: 
                                      In order to provide reliable guarantees as to the behavior of services on the bus we must know the exact semantics of each tessage sent. 
                                      Some combinations of inherited interfaces would present contradictions which would make it impossible for the bus to know how to act.
                                      Some inherited interfaces absolutely require that concrete types implement some other interface.
                                      It is quite easy to miss this when designing your types unless you have help.
                                      We provide this help by detecting these mistakes and throwing runtime exceptions.

                                      """;
}
