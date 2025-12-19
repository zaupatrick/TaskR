using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using TaskR.Data;
using TaskR.Services;

namespace TaskR
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            builder.Services.AddDbContext<MyDbContext>
                (options => options
                    .UseSqlServer(builder.Configuration.GetConnectionString("AppDb") ?? throw new InvalidOperationException("Connection string 'AppDb' not found.")));

            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<TodoService>();
            builder.Services.AddScoped<AufgabenService>();
            builder.Services.AddScoped<TagService>();

            builder.Services.AddAuthentication(
                CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(opts =>
                {
                    opts.LoginPath = "/Auth/Logon";
                    opts.LogoutPath = "/Auth/Logout";
                    opts.AccessDeniedPath = "/Home/Index";
                    
                });

            //builder.WebHost.UseUrls("http://192.168.0.4:5000");
            //builder.WebHost.UseUrls("http://172.25.96.1:5000");

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
