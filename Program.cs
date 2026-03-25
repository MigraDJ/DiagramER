using DiagramER.Components;
using DiagramER.Services;

namespace DiagramER
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // Add theme service
            builder.Services.AddScoped<ThemeService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // Security Headers Middleware
            app.Use(async (context, next) =>
            {
                // Content Security Policy - prevents XSS attacks
                context.Response.Headers.Add("Content-Security-Policy", 
                    "default-src 'self'; " +
                    "script-src 'self' https://cdn.jsdelivr.net; " +
                    "style-src 'self' 'unsafe-inline'; " +
                    "img-src 'self' data: https:; " +
                    "font-src 'self'; " +
                    "connect-src 'self'; " +
                    "frame-ancestors 'none'; " +
                    "base-uri 'self'; " +
                    "form-action 'self'");

                // X-Content-Type-Options - prevents MIME sniffing
                context.Response.Headers.Add("X-Content-Type-Options", "nosniff");

                // X-Frame-Options - prevents clickjacking
                context.Response.Headers.Add("X-Frame-Options", "DENY");

                // X-XSS-Protection - legacy XSS protection (in addition to CSP)
                context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");

                // Referrer-Policy - controls referrer information
                context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

                // Permissions-Policy - restricts browser features
                context.Response.Headers.Add("Permissions-Policy", 
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

            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
            app.UseHttpsRedirection();

            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
