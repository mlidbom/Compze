namespace Compze.Tessaging.Typermedia;

///<remarks>bug: "Remote exception tessage" reads as a rename of message to tessage that reached the wrong word: what follows is the<br/>
/// handler's exception as text, not a tessage. (website-and-docs, 2026-11-24)</remarks>
///<remarks>refactor: The handler's exception crosses only as its text, so a caller can't tell one failure from another by type,<br/>
/// and the remote stack trace is only part of a message. (website-and-docs, 2026-11-24)</remarks>
public class TessageDispatchingFailedException(string remoteExceptionAsString) : Exception($"""
                                                                                            Dispatching tessage failed. Remote exception tessage: 
                                                                                            {remoteExceptionAsString} 
                                                                                            """);
