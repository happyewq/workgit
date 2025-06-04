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
        public static void Register(IWebHostEnvironment env, IConfiguration config)
        {
            try
            {
                // ✅ 建立服務實體
                var batchService = new OchBatchService1(config);

                // ✅ 根據作業系統取得正確的台灣時區
                var taiwanTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Taipei Standard Time" : "Asia/Taipei"
                );

                // ✅ 每天 11:40 台灣時間發送群組提醒
                RecurringJob.AddOrUpdate<OchBatchService1>(
                    service => service.SendLine(),
                    "40 11 * * *",
                    taiwanTimeZone
                );

                // ✅ 每天 09:00 台灣時間推播未讀經名單
                RecurringJob.AddOrUpdate<OchBatchService1>(
                    service => service.SendUnReadYesterdayAsync(),
                    "0 9 * * *",
                    taiwanTimeZone
                );

                Console.WriteLine("✅ Cron Job 註冊完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Cron Job 註冊失敗：" + ex.Message);
            }
        }
    }
}
