using DiagramER.Components;
using DiagramER.Extensions;
using DiagramER.Services;

namespace DiagramER
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services
            builder.Services
                .AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.Services.AddScoped<ThemeService>();
            builder.AddCompressionServices();

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
                app.UseResponseCompression();
            }
            else
            {
                app.UseDevCompressionHandling();
            }

            app.UseSecurityHeaders()
               .UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true)
               .UseHttpsRedirection()
               .UseAntiforgery();

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
               .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
