using CcpBatch.Jobs;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
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
            // ✅ 避免金鑰錯誤（Session、AntiForgery）
            services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "keys")))
                .SetApplicationName("ochweb");

            // ✅ Hangfire 設定（控制連線池與逾時）
            services.AddHangfire(config =>
            {
                var connStr = Environment.GetEnvironmentVariable("DefaultConnection");
                config.UsePostgreSqlStorage(connStr, new PostgreSqlStorageOptions
                {
                    QueuePollInterval = TimeSpan.FromSeconds(30),     // 降低輪詢頻率
                    InvisibilityTimeout = TimeSpan.FromMinutes(5),    // 任務鎖定時間
                    PrepareSchemaIfNecessary = true,                  // 自動建立 Hangfire 表（可選）
                    DistributedLockTimeout = TimeSpan.FromMinutes(1)  // 鎖定逾時保守設定
                });
            });

            // ✅ 限制 worker，避免連線爆掉
            services.AddHangfireServer(options =>
            {
                options.WorkerCount = 5;
            });

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

        public class AllowAllDashboardAuthorizationFilter : IDashboardAuthorizationFilter
        {
            public bool Authorize(DashboardContext context) => true;
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseExceptionHandler("/Home/Error");

            // ✅ 初始化資料庫連線設定
            ochweb.Helpers.DBHelper.Init(Configuration);

            app.UseStaticFiles();
            app.UseRouting();
            app.UseSession();
            app.UseAuthorization();

            // ✅ 啟用 Hangfire Dashboard
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = new[] { new AllowAllDashboardAuthorizationFilter() },
                IgnoreAntiforgeryToken = true
            });


            // ✅ 註冊排程任務，加上 try-catch 防止啟動失敗
            try
            {
                CronJobConfig.Register(env, Configuration);
            }
            catch (Exception ex)
            {
                Console.WriteLine("⚠️ Cron 任務註冊失敗：" + ex.Message);
            }

            // Swagger 設定
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

                endpoints.MapHangfireDashboard();
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
