namespace ConnectedOps.Domain.Security;

public enum SecurityEventType
{
    LoginSucceeded = 1,
    LoginFailed = 2,
    AccountLocked = 3,

    TenantSelectionSucceeded = 10,
    TenantSelectionFailed = 11,

    RefreshTokenRotated = 20,
    RefreshTokenReuseDetected = 21,

    Logout = 30,
    LogoutAll = 31,

    UnauthorizedAccess = 40,
    ForbiddenAccess = 41,

    InvalidAccessToken = 50,
    ExpiredAccessToken = 51,

    InvalidTenantContext = 60,

    SuspiciousActivity = 100
}