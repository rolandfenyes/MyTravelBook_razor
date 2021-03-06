using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyTravelBook.Dal;
using MyTravelBook.Dal.Entities;
using MyTravelBook.Dal.Roles;
using MyTravelBook.Dal.SeedInterfaces;
using MyTravelBook.Dal.SeedService;
using MyTravelBook.Dal.Services;
using MyTravelBook.Web.Services;
using MyTravelBook.Web.Settings;

namespace MyTravelBook.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages()
                .AddRazorRuntimeCompilation();

            services.AddDbContext<MyDbContext>(
                o => o.UseSqlServer(Configuration.GetConnectionString(nameof(MyDbContext)))
                );

            services.AddIdentity<User, IdentityRole<int>>()
                .AddEntityFrameworkStores<MyDbContext>()
                .AddDefaultTokenProviders();

            services.AddAuthentication()
                .AddFacebook(facebookOptions =>
                {
                    facebookOptions.AppId = Configuration["Authentication:Facebook:AppId"];
                    facebookOptions.AppSecret = Configuration["Authentication:Facebook:AppSecret"];
                });

            // SMTP server properties from appsettings.json/MailSettings
            services.Configure<MailSettings>(Configuration.GetSection("MailSettings"));

            services.AddTransient<IEmailSender, EmailSender>();

            services.AddScoped<IRoleSeedService, RoleSeedService>()
                .AddScoped<IUserSeedService, UserSeedService>();

            services.AddScoped<TripService>()
                .AddScoped<TravelService>()
                .AddScoped<AccommodationService>()
                .AddScoped<ExpenseService>();

            services.AddAuthorization(options =>
            {
                options.AddPolicy("User", policy => policy.RequireRole(Roles.User));
            });

            services.AddRazorPages(options =>
            {
                options.Conventions.AuthorizeFolder("/Add", "User");
                options.Conventions.AuthorizeFolder("/Details", "User");
                options.Conventions.AuthorizeFolder("/Edit", "User");
                options.Conventions.AuthorizePage("/CreateTrip", "User");
                options.Conventions.AuthorizePage("/Trip", "User");
                options.Conventions.AuthorizePage("/Friends", "User");
            });

            services.ConfigureApplicationCookie(options =>
            {
                options.Events = new Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationEvents
                {

                    OnRedirectToAccessDenied = ctx =>
                    {
                        ctx.Response.Redirect("/Errors/Error");
                        return Task.CompletedTask;
                    }
                };
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}
