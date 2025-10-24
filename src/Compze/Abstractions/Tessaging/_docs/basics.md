# Tessaging Basics

## Message Handling

Here's a simple example of handling a message:

[!code-csharp[](MessageHandling.cs#message_handler_example)]

## Using Message Handler Interface

For dependency injection support, wrap handlers in interfaces:

[!code-csharp[](MessageHandling.cs#message_handler_interface)]

This allows your IoC container to resolve and inject dependencies into your handlers.
