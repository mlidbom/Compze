﻿using System;
using Compze.Contracts;

namespace Compze.Testing.Threading;

static class GatedCodeSectionExtensions
{
   public static IGatedCodeSection Open(this IGatedCodeSection @this)
   {
      @this.EntranceGate.Open();
      @this.ExitGate.Open();
      return @this;
   }

   public static IGatedCodeSection Close(this IGatedCodeSection @this)
   {
      @this.EntranceGate.Close();
      @this.ExitGate.Close();
      return @this;
   }

   public static IGatedCodeSection LetOneThreadEnter(this IGatedCodeSection @this)
   {
      @this.AssertIsEmpty();
      @this.EntranceGate.AwaitLetOneThreadPassThrough();
      return @this;
   }

   public static IGatedCodeSection LetOneThreadEnterAndReachExit(this IGatedCodeSection @this)
   {
      @this.LetOneThreadEnter();
      @this.ExitGate.AwaitQueueLengthEqualTo(1);
      return @this;
   }

   public static IGatedCodeSection LetOneThreadPass(this IGatedCodeSection @this)
   {
      @this.LetOneThreadEnterAndReachExit();
      @this.ExitGate.AwaitLetOneThreadPassThrough();
      return @this;
   }

   public static void Execute(this IGatedCodeSection @this, Action action)
   {
      using(@this.Enter())
      {
         action();
      }
   }

   public static bool IsEmpty(this IGatedCodeSection @this)
      => @this.EntranceGate.Passed == @this.ExitGate.Passed;

   public static IGatedCodeSection AssertIsEmpty(this IGatedCodeSection @this)
   {
      Assert.State.Is(@this.IsEmpty(), () => $"{nameof(IGatedCodeSection)} must be empty when calling this method");
      return @this;
   }
}