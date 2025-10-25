using System;

namespace Compze.Tessaging.Implementation.TessageHandling.Dispatching;

public class TessageDispatchingFailedException(string remoteExceptionAsString) : Exception($"""
                                                                                            Dispatching tessage failed. Remote exception tessage: 
                                                                                            {remoteExceptionAsString} 
                                                                                            """);