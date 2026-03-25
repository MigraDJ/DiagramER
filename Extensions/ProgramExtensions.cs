using DiagramER.Services;

namespace DiagramER.Extensions
{
    public static class ProgramExtensions
    {
        /// <summary>
        /// Adds compression services configured for the current environment.
        /// In development, compression is skipped. In production, it's enabled.
        /// </summary>
        public static WebApplicationBuilder AddCompressionServices(this WebApplicationBuilder builder)
        {
            if (!builder.Environment.IsDevelopment())
            {
                builder.Services.AddResponseCompression(options =>
                {
                    options.EnableForHttps = true;
                    options.ExcludedMimeTypes = options.ExcludedMimeTypes.Concat(new[] { "text/html" }).ToArray();
                });
            }

            return builder;
        }

        /// <summary>
        /// Adds security headers middleware to the application.
        /// </summary>
        public static WebApplication UseSecurityHeaders(this WebApplication app)
        {
            app.Use(async (context, next) =>
            {
                //context.Response.Headers.Append("Content-Security-Policy",
                //    "default-src 'self'; " +
                //    "script-src 'self' https://cdn.jsdelivr.net; " +
                //    "style-src 'self' 'unsafe-inline'; " +
                //    "img-src 'self' data: https:; " +
                //    "font-src 'self'; " +
                //    "connect-src 'self'; " +
                //    "frame-ancestors 'none'; " +
                //    "base-uri 'self'; " +
                //    "form-action 'self'");

                context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Append("X-Frame-Options", "DENY");
                context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
                context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
                context.Response.Headers.Append("Permissions-Policy",
                    "geolocation=(), " +
                    "microphone=(), " +
                    "camera=(), " +
                    "payment=(), " +
                    "usb=(), " +
                    "magnetometer=(), " +
                    "gyroscope=(), " +
                    "accelerometer=()");

                await next.Invoke();
            });

            return app;
        }

        /// <summary>
        /// Configures compression handling for development environment.
        /// Disables compression to prevent browser refresh middleware issues.
        /// </summary>
        public static WebApplication UseDevCompressionHandling(this WebApplication app)
        {
            app.UseExceptionHandler(exceptionHandlerApp =>
            {
                exceptionHandlerApp.Run(async context =>
                {
                    var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;

                    if (exception is System.IO.InvalidDataException &&
                        exception.Message.Contains("compressed using an unsupported compression method"))
                    {
                        context.Response.StatusCode = 200;
                        context.Response.ContentType = "text/html; charset=utf-8";
                        await context.Response.WriteAsync("<html><body><h1>Server Error</h1><p>Browser refresh encountered a compression error. Please refresh the page.</p></body></html>");
                        return;
                    }

                    throw exception ?? new Exception("Unknown error");
                });
            });

            app.Use(async (context, next) =>
            {
                context.Request.Headers["Accept-Encoding"] = "identity";
                await next();
            });

            return app;
        }
    }
}
