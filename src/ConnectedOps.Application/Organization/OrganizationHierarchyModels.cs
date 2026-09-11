namespace ConnectedOps.Application.Organization;

public sealed record OrganizationHierarchyNodeDto(
    Guid Id,
    string Name,
    string NodeType,
    string? Code,
    string? SubTitle,
    IReadOnlyCollection<OrganizationHierarchyNodeDto> Children);
