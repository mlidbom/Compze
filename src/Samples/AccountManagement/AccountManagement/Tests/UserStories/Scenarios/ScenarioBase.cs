using System;
using AccountManagement.API;
using Compze.Utilities.Testing.Must;

namespace AccountManagement.UserStories.Scenarios;

public abstract class ScenarioBase<TResult>
{
   protected AccountApi Api => AccountApi.Instance;

   public abstract TResult Execute();


   public CaughtException<TException> ExecutingShouldThrow<TException>() where TException : Exception => this.Invoking(it => it.Execute()).Must().Throw<TException>();
}