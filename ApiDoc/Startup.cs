using System; 
using System.IO;
using System.Reflection;
using ApiDoc.DAL; 
using ApiDoc.Middleware;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.PlatformAbstractions;

namespace ApiDoc
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
            services.AddControllersWithViews();
            services.AddSession();

            //DAL 
            
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            //�������ע��ʵ����AutofacModuleRegister�ͼ̳���Autofac.Module����
            builder.RegisterModule(new AutofacModuleRegister());

            //û�нӿ�
            //var assemblysServicesNoInterfaces = Assembly.Load("ApiDoc.DAL");
            //builder.RegisterAssemblyTypes(assemblysServicesNoInterfaces);  

          
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseMiddleware<DBMiddleware>();
            app.UseMiddleware<CorsMiddleware>();
            app.UseSession();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot"))
            });

            app.UseRouting();
            //app.Use((context,next)=> {
            //    var endpoint = context.GetEndpoint();
            //    var route = context.Request.RouteValues;
            //    return next(); 
            //});
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
