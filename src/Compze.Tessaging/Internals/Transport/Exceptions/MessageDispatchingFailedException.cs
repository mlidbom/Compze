namespace Compze.Tessaging.Internals.Transport.Exceptions;

public class MessageDispatchingFailedException(string remoteExceptionAsString) : Exception($"""
                                                                                            Dispatching tessage failed. Remote exception tessage: 
                                                                                            {remoteExceptionAsString} 
                                                                                            """);
