using CcpBatch.Jobs;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ochweb
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

            // 註冊 Hangfire，使用 PostgreSQL 作為儲存（請根據你的資料庫修改連線字串）
            services.AddHangfire(config =>
                config.UsePostgreSqlStorage(Configuration.GetConnectionString("DefaultConnection")));

            // 啟用背景工作伺服器
            services.AddHangfireServer();
            services.AddControllersWithViews()
                .AddRazorRuntimeCompilation(); // ✅ 加這行;
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

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            // ✅ 初始化DBHelper
            ochweb.Helpers.DBHelper.Init(Configuration);

            app.UseStaticFiles();

            app.UseRouting();

            app.UseSession();

            app.UseAuthorization();

            // 啟用 Hangfire Dashboard（可加權限）
            app.UseHangfireDashboard("/hangfire");
            // ✅ 加這一行！不然 Dashboard 會顯示「沒有執行中的伺服器」
            app.UseHangfireServer();

            // 🔻 這行必須加上（你目前可能沒呼叫這個方法）
            CronJobConfig.Register(env, Configuration);


            app.UseSwagger(); // 加這行：產生 swagger.json
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "OCH API v1");
                c.RoutePrefix = "swagger"; // 存取網址為 /swagger
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Login}/{action=Index}/{id?}");
            });
            // 額外開放 Script 資料夾裡面放JS
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
