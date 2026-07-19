using Compze.DependencyInjection.Runtime;
using Compze.DependencyInjection.Runtime.Resolution;
using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;

using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming

namespace Compze.DependencyInjection.Specifications.UnitOfWork_execution;

public class When_executing_a_unit_of_work
{
   [DependencyInjectionContainerMatrix]
   public void the_resolvers_Id_is_stable_within_the_unit_of_work_and_differs_between_units_of_work()
   {
      using var container = CreateContainer();

      UnitOfWorkId firstUnitOfWorkId = null!;
      container.ExecuteUnitOfWork(unitOfWork =>
      {
         firstUnitOfWorkId = unitOfWork.Id;
         unitOfWork.Id.Must().Be(firstUnitOfWorkId);
      });

      container.ExecuteUnitOfWork(unitOfWork => unitOfWork.Id.Must().NotBe(firstUnitOfWorkId));
   }

   [DependencyInjectionContainerMatrix]
   public void an_OnCommittedSuccessfully_action_runs_after_the_unit_of_work_commits_never_before()
   {
      using var container = CreateContainer();

      var hasRun = false;
      container.ExecuteUnitOfWork(unitOfWork =>
      {
         unitOfWork.OnCommittedSuccessfully(() => hasRun = true);
         hasRun.Must().BeFalse();
      });

      hasRun.Must().BeTrue();
   }

   [DependencyInjectionContainerMatrix]
   public void an_OnCommittedSuccessfully_action_never_runs_when_the_unit_of_work_rolls_back()
   {
      using var container = CreateContainer();

      var hasRun = false;
      Invoking(() => container.ExecuteUnitOfWork(unitOfWork =>
              {
                 unitOfWork.OnCommittedSuccessfully(() => hasRun = true);
                 throw new Exception("roll back the unit of work");
              }))
             .Must().Throw<Exception>();

      hasRun.Must().BeFalse();
   }

   [DependencyInjectionContainerMatrix]
   public void an_OnCompleted_action_runs_when_the_unit_of_work_commits_and_when_it_rolls_back()
   {
      using var container = CreateContainer();

      var completions = 0;
      container.ExecuteUnitOfWork(unitOfWork => unitOfWork.OnCompleted(() => completions++));
      completions.Must().Be(1);

      Invoking(() => container.ExecuteUnitOfWork(unitOfWork =>
              {
                 unitOfWork.OnCompleted(() => completions++);
                 throw new Exception("roll back the unit of work");
              }))
             .Must().Throw<Exception>();
      completions.Must().Be(2);
   }

   static IDependencyInjectionContainer CreateContainer() => DependencyInjectionContainerFactory.CreateContainerBuilder().Build();
}
