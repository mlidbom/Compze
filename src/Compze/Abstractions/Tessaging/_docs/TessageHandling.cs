// ReSharper disable All

using Compze.Tessaging.Teventive.TeventStore.Abstractions;

#pragma warning disable

namespace Compze.Tessaging.Abstractions;

/// <summary>
/// Example code for documentation purposes - demonstrates basic tessage handling patterns.
/// This file is visible and refactorable in production projects but only compiled into the Website project.
/// </summary>
class TessageHandlingExamples
{
   #region tessage_handler_example
   void Handle(MyTommand tommand)
   {
      // Process the tommand
   }
   #endregion

   #region tessage_handler_interface
   class MyTommandHandler : ITessageHandler<MyTommand>
   {
      public void Handle(MyTommand tommand)
      {
         // Process the tommand
      }
   }
   #endregion
}

class MyTommand {}

interface IMyTevent : ITaggregateTevent
{

}

interface ITessageHandler<T>
{
   void Handle(T tommand);
}
