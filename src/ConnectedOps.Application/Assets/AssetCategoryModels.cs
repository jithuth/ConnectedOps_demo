namespace ConnectedOps.Application.Assets;

public sealed record AssetCategoryDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    string? ParentCategoryName,
    bool IsActive,
    int AssetCount,
    int SubCategoryCount,
    DateTime CreatedAtUtc);

public sealed record AssetCategoryTreeDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    bool IsActive,
    int AssetCount,
    List<AssetCategoryTreeDto> Children);

public sealed record CreateAssetCategoryRequest(
    string Code,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    bool IsActive = true);

public sealed record UpdateAssetCategoryRequest(
    string Code,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    bool IsActive);
