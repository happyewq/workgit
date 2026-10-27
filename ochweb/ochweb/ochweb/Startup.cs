using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;

namespace ochweb
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // 避免金鑰錯誤（Session、AntiForgery）
            services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "keys")))
                .SetApplicationName("ochweb");

            // 暫時停用 Hangfire
            // services.AddHangfire(...)
            // services.AddHangfireServer(...)

            services.AddControllersWithViews()
                .AddRazorRuntimeCompilation();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "OCH API 文件",
                    Version = "v1",
                    Description = "我們教會管理系統的 API 文件"
                });
            });

            services.AddSession();
            services.AddHttpContextAccessor();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseExceptionHandler("/Home/Error");

            // 初始化資料庫連線設定
            ochweb.Helpers.DBHelper.Init(Configuration);

            app.UseStaticFiles();
            app.UseRouting();
            app.UseSession();
            app.UseAuthorization();

            // 暫時停用 Hangfire Dashboard
            // app.UseHangfireDashboard("/hangfire", new DashboardOptions
            // {
            //     Authorization = new[] { new AllowAllDashboardAuthorizationFilter() },
            //     IgnoreAntiforgeryToken = true
            // });

            // 暫時停用排程註冊
            // try
            // {
            //     CronJobConfig.Register(env, Configuration);
            // }
            // catch (Exception ex)
            // {
            //     Console.WriteLine("⚠️ Cron 任務註冊失敗：" + ex.Message);
            // }

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "OCH API v1");
                c.RoutePrefix = "swagger";
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Login}/{action=Index}/{id?}");

                // 暫時停用 Hangfire Dashboard 路由
                // endpoints.MapHangfireDashboard();
            });

            // 額外開放 Script 資料夾（自訂 JS）
            var scriptPath = Path.Combine(env.ContentRootPath, "Script");
            if (Directory.Exists(scriptPath))
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(scriptPath),
                    RequestPath = "/Script"
                });
            }
        }
    }
}