using System;

namespace Compze.Tessaging.Tessaging.Buses.Implementation;

public class MessageDispatchingFailedException(string remoteExceptionAsString) : Exception($"""
                                                                                            Dispatching message failed. Remote exception message: 
                                                                                            {remoteExceptionAsString} 
                                                                                            """);