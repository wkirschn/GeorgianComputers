using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using GeorgianComputers.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using GeorgianComputers.Models;

namespace GeorgianComputers
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
            services.AddDbContext<GeorgianComputersContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection")));
            // Configure Identity to work without DBase
            // Use our new ApplicationRole class to manage Roles and permissions
            // Point Identity to the existing GeorgianComputer DBase Context Class
            // Use default cookie settings
            
            services.AddIdentity<ApplicationUser, ApplicationRole>()
                  .AddDefaultUI()
                  .AddRoles<ApplicationRole>()
                  .AddRoleManager<RoleManager<ApplicationRole>>()
                  .AddEntityFrameworkStores<GeorgianComputersContext>()
                  .AddDefaultTokenProviders();
            
            //Replace with AddIdentity
            /*services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<GeorgianComputersContext>();*/
            services.AddControllersWithViews();
            services.AddRazorPages();


            //1. Add Session Support
            services.AddSession();

            //Stripe//
            //Make Configuration Value from appsettings.json avaliable to the controller
            services.AddSingleton<IConfiguration>(Configuration);


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            //2. Enable Session Support

           app.UseSession();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }
    }
}
