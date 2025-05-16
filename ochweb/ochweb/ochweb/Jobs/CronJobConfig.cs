using Hangfire;
using ochweb.OchBatchService;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Runtime.InteropServices;

namespace CcpBatch.Jobs
{
    public class CronJobConfig
    {
        private readonly IConfiguration _config;
        public static void Register(IWebHostEnvironment env, IConfiguration config)
        {

            OchBatchService1 NewCcpBatchService = new OchBatchService1(config);

            // 如果是線上系統區域，註冊定期任務 每天16點20分
            var taiwanTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Taipei Standard Time" : "Asia/Taipei"
            );

            RecurringJob.AddOrUpdate<OchBatchService1>(
                service => service.SendLine(),
                "40 11 * * *",
                taiwanTimeZone
            );
        }
    }
}
