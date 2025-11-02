using System;
using AccountManagement.API;
using Compze.Tests.Infrastructure.Fluent;
using static Compze.Tests.Infrastructure.Fluent.MustActions;


namespace AccountManagement.UserStories.Scenarios;

public abstract class ScenarioBase<TResult>
{
   protected AccountApi Api => AccountApi.Instance;

   public abstract TResult Execute();


   public CaughtException<TException> ExecutingShouldThrow<TException>() where TException : Exception => this.Invoking(it => it.Execute()).Must().Throw<TException>();
}