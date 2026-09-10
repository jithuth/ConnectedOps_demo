using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Authorization;

public sealed class Permission : BaseEntity
{
    private Permission()
    {
    }

    public Permission(
        string key,
        string name,
        string module,
        string? description = null)
    {
        SetKey(key);
        SetName(name);
        SetModule(module);

        Description = description?.Trim();
        IsActive = true;
    }

    public string Key { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Module { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public void SetKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException(
                "Permission key is required.",
                nameof(key));

        Key = key.Trim();
        MarkUpdated();
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Permission name is required.",
                nameof(name));

        Name = name.Trim();
        MarkUpdated();
    }

    public void SetModule(string module)
    {
        if (string.IsNullOrWhiteSpace(module))
            throw new ArgumentException(
                "Permission module is required.",
                nameof(module));

        Module = module.Trim();
        MarkUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }
}