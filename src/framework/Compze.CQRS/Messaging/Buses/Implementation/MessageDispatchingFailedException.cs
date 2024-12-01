using System;

namespace Composable.Messaging.Buses.Implementation;

public class MessageDispatchingFailedException(string remoteExceptionAsString) : Exception($"""
                                                                                            Dispatching message failed. Remote exception message: 
                                                                                            {remoteExceptionAsString} 
                                                                                            """);