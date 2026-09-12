namespace ConnectedOps.Application.Common.Exceptions;

public sealed class NotFoundException : Exception
{
    public NotFoundException(string entityName, string key)
        : base($"Entity '{entityName}' ({key}) was not found.")
    {
    }

    public NotFoundException(string message)
        : base(message)
    {
    }
}
