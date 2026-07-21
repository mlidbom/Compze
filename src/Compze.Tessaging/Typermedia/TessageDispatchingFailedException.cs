namespace Compze.Tessaging.Typermedia;

public class TessageDispatchingFailedException(string remoteExceptionAsString) : Exception($"""
                                                                                            Dispatching tessage failed. Remote exception tessage: 
                                                                                            {remoteExceptionAsString} 
                                                                                            """);
