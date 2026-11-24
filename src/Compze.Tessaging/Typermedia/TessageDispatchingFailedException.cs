namespace Compze.Tessaging.Typermedia;

///<summary>Thrown to the sender of a typermedia tessage when the remote endpoint's handler for it threw: the message holds that exception as text.</summary>
public class TessageDispatchingFailedException(string remoteExceptionAsString) : Exception($"""
                                                                                            Dispatching tessage failed. Remote exception: 
                                                                                            {remoteExceptionAsString} 
                                                                                            """)
{
   //refactor: The handler's exception crosses only as its text, so a caller can't tell one failure from another by type,
   //   and the remote stack trace is only part of a message. (website-and-docs, 2026-11-24)
}
