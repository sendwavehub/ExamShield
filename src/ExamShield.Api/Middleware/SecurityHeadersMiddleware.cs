namespace ExamShield.Api.Middleware;

public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var h = context.Response.Headers;

        // Prevent MIME-type sniffing
        h["X-Content-Type-Options"] = "nosniff";

        // Disallow framing — complements CSP frame-ancestors
        h["X-Frame-Options"] = "DENY";

        // Disable legacy XSS filter (CSP is the modern replacement)
        h["X-XSS-Protection"] = "0";

        // Limit referrer information sent to cross-origin requests
        h["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Restrict browser feature access — camera and mic are controlled by the Flutter app only
        h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

        // Content Security Policy
        // - default-src 'self': block anything not explicitly allowed
        // - connect-src wss:: allow SignalR WebSocket upgrade on same origin
        // - img-src data: blob:: thumbnails and QR codes rendered client-side
        h["Content-Security-Policy"] =
            "default-src 'self'; " +
            "script-src 'self'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: blob:; " +
            "connect-src 'self' wss: ws:; " +
            "font-src 'self'; " +
            "object-src 'none'; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'";

        // HSTS — 1 year, include subdomains, eligible for preload list
        // Only injected on HTTPS; browsers ignore HSTS on plain HTTP.
        if (context.Request.IsHttps)
            h["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";

        await next(context);
    }
}
