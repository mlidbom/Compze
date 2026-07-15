# Tessaging Basics

## Tessage handling

Here's a simple example of handling a tessage:

[!code-csharp[](TessageHandling.cs#tessage_handler_example)]

## Using a tessage handler interface

For dependency injection support, wrap handlers in interfaces:

[!code-csharp[](TessageHandling.cs#tessage_handler_interface)]

This allows your IoC container to resolve and inject dependencies into your handlers.
