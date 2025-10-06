using System;

namespace Compze.Tessaging.Hosting.Implementation;

public class MessageDispatchingFailedException(string remoteExceptionAsString) : Exception($"""
                                                                                            Dispatching message failed. Remote exception message: 
                                                                                            {remoteExceptionAsString} 
                                                                                            """);