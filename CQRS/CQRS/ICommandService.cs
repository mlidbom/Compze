namespace Composable.CQRS
{
    public interface ICommandService
    {
        void Execute<TCommand>(TCommand command);
    }
}