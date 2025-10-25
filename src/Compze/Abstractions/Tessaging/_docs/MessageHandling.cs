// ReSharper disable All

using Compze.Tessaging.Teventive.EventStore.Abstractions;

#pragma warning disable

namespace Compze.Tessaging.Abstractions;

/// <summary>
/// Example code for documentation purposes - demonstrates basic tessage handling patterns.
/// This file is visible and refactorable in production projects but only compiled into the Website project.
/// </summary>
class TessageHandlingExamples
{
   #region tessage_handler_example
   void Handle(MyCommand command)
   {
      // Process the command
   }
   #endregion

   #region tessage_handler_interface
   class MyCommandHandler : ITessageHandler<MyCommand>
   {
      public void Handle(MyCommand command)
      {
         // Process the command
      }
   }
   #endregion
}

class MyCommand {}

interface IMyEvent : IAggregateEvent
{

}

interface ITessageHandler<T>
{
   void Handle(T command);
}
