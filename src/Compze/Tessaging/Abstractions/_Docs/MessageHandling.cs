// ReSharper disable All
#pragma warning disable

namespace Compze.Tessaging.Abstractions;

/// <summary>
/// Example code for documentation purposes - demonstrates basic message handling patterns.
/// This file is visible and refactorable in production projects but only compiled into the Website project.
/// </summary>
class MessageHandlingExamples
{
   #region message_handler_example
   void Handle(MyCommand command)
   {
      // Process the command
   }
   #endregion

   #region message_handler_interface
   class MyCommandHandler : IMessageHandler<MyCommand>
   {
      public void Handle(MyCommand command)
      {
         // Process the command
      }
   }
   #endregion
}

class MyCommand {}

interface IMessageHandler<T>
{
   void Handle(T command);
}
