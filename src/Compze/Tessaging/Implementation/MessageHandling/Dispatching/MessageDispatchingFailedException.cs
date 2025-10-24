using System;

namespace Compze.Tessaging.Implementation.MessageHandling.Dispatching;

public class MessageDispatchingFailedException(string remoteExceptionAsString) : Exception($"""
                                                                                            Dispatching message failed. Remote exception message: 
                                                                                            {remoteExceptionAsString} 
                                                                                            """);