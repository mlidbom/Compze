using System.Collections.Concurrent;
using Compze.Internals.SystemCE;
using Compze.Must;


// ReSharper disable All
#pragma warning disable //Reviewed OK: This is API experimental code that is never ever used.

namespace Compze.Tests.ScratchPad.APIDraft.Policyv2;

public class Testing
{
   public void Test()
   {
      var createAccountHandler = new TestTessageHandler<CreateAccountTommand>();
      var accountQueryModelUpdater = new TestTessageHandler<AccountCreatedTevent>();

      var endpoint = new Endpoint(
         TommandHandler.For<CreateAccountTommand>("BE8B06E7-28BB-439D-BDD6-CF7E9454424B", createAccountHandler.Handle),
         TeventHandler.For<AccountCreatedTevent>("AD198D3E-5340-4CB3-8BDB-31AFD0C7FC9A", accountQueryModelUpdater.Handle)
      );

      //bus.SendAsyncAsync(new CreateAccountTommand)
      createAccountHandler.Started.Wait();
      accountQueryModelUpdater.IsStarted.Must().Be(false);
      createAccountHandler.AllowToComplete.Set();
      accountQueryModelUpdater.Started.Wait();
   }



   class TestingResetTevent
   {
      private readonly ManualResetEventSlim _tevent = new ManualResetEventSlim(false);
      private readonly TimeSpan timeout_;
      private readonly string name_;

      public TestingResetTevent(string name, TimeSpan? timeout = null)
      {
         timeout_ = timeout ?? 1.Seconds();
         name_ = name;
      }

      public void Wait()
      {
         if (!_tevent.Wait(timeout_))
         {
            throw new Exception($"Timed out waiting for lock: {name_}");
         }
      }

      public void Set() => _tevent.Set();
      public void Reset() => _tevent.Reset();
   }


   class TestingResetTeventCollection
   {
      private readonly ConcurrentDictionary<string, TestingResetTevent> _manuals = new ConcurrentDictionary<string, TestingResetTevent>();

      private TimeSpan timeout_;
      public TestingResetTeventCollection(TimeSpan? timeout = null)
      {
         timeout_ = timeout ?? 1.Seconds();
      }

      public TestingResetTevent Manual(string name)
      {
         lock (_manuals)
         {
            return _manuals.GetOrAdd(name, _ => new TestingResetTevent(name, timeout_));
         }
      }

      public TestingResetTevent Manual(int key) => Manual(key.ToString());
      public TestingResetTevent Manual(Guid key) => Manual(key.ToString());
   }



   //Register a handler implemented like this and you get full insight into when it is invoked, and full control over when it is allowed to complete.
   //This should give us full testability of invokation policies :)
   class TestTessageHandler<T>
   {
      public readonly TestingResetTevent Started = new TestingResetTevent(nameof(Started));
      public readonly  TestingResetTevent Completed = new TestingResetTevent(nameof(Completed));
      public  readonly  TestingResetTevent AllowToComplete = new TestingResetTevent(nameof(AllowToComplete));

      public bool IsStarted = false;
      public bool IsCompleted = false;
      public bool IsRunning => IsStarted && !IsCompleted;


      public void Handle(T tessage)
      {
         Completed.Reset();
         IsCompleted = false;
         Started.Set();
         IsStarted = true;
                

         AllowToComplete.Wait();
         AllowToComplete.Reset();

         Completed.Set();
         IsCompleted = true;                
         Started.Reset();
         IsStarted = false;                
      }
   }
}
