using System;

namespace Compze.Messaging.Buses.Implementation;

public class MessageDispatchingFailedException(string remoteExceptionAsString) : Exception($"""
                                                                                            Dispatching message failed. Remote exception message: 
                                                                                            {remoteExceptionAsString} 
                                                                                            """);