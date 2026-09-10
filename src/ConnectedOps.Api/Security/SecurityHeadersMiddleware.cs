namespace ConnectedOps.Api.Security;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(
        RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] =
            "nosniff";

        headers["X-Frame-Options"] =
            "DENY";

        headers["Referrer-Policy"] =
            "no-referrer";

        headers["Permissions-Policy"] =
            "camera=(), microphone=(), geolocation=()";

        headers["Cross-Origin-Opener-Policy"] =
            "same-origin";

        headers["Cross-Origin-Resource-Policy"] =
            "same-site";

        // API responses can contain sensitive tenant/user data.
        headers["Cache-Control"] =
            "no-store, no-cache, must-revalidate";

        headers["Pragma"] =
            "no-cache";

        await _next(context);
    }
}