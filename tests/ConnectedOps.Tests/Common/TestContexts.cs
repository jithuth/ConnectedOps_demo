using System.Security.Claims;
using ConnectedOps.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ConnectedOps.Tests.Common;

public sealed class TestTenantContext : ICurrentTenantContext
{
    public bool IsAuthenticated => UserId.HasValue;
    public Guid? UserId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? TenantUserId { get; set; }
}

public sealed class TestUserContext : ICurrentUserContext
{
    public bool IsAuthenticated => UserId.HasValue;
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? TenantUserId { get; set; }
}

public static class TestHttpContextHelper
{
    public static IHttpContextAccessor CreateHttpContextAccessor(ClaimsPrincipal? user = null, string ip = "127.0.0.1")
    {
        var context = new DefaultHttpContext();
        if (user != null)
        {
            context.User = user;
        }
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(ip);

        return new HttpContextAccessor { HttpContext = context };
    }
}
