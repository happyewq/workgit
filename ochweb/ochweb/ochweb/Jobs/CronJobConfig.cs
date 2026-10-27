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

                // ✅ 跨平台取得台灣時區（Linux: Asia/Taipei, Windows: Taipei Standard Time）
                TimeZoneInfo taiwanTimeZone;
                try
                {
                    taiwanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");
                }
                catch (TimeZoneNotFoundException)
                {
                    taiwanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
                }

                // ⛔ 已停用：每天 11:40 私訊「請繳費」
                // 排程存在 Hangfire 資料庫裡，要主動刪除，不然之前註冊的還是會跑
                RecurringJob.RemoveIfExists("OchBatchService1.SendLine");

                // ✅ 每天 09:00 台灣時間推播未讀經名單
                //RecurringJob.AddOrUpdate<OchBatchService1>(
                //    service => service.SendUnReadYesterdayAsync(),
                //    "0 9 * * *",
                //    taiwanTimeZone
                //);

                // ✅ 每天 10:00 台灣時間：連續沒讀滿 3、6、9…天的人私訊提醒
                RecurringJob.AddOrUpdate<OchBatchService1>(
                    service => service.SendUnReadThreeDaysAsync(),
                    "0 10 * * *",
                    taiwanTimeZone
                );

                // ✅ 每月 1 號 10:00 台灣時間：上個月全勤的人私訊恭喜
                RecurringJob.AddOrUpdate<OchBatchService1>(
                    service => service.SendMonthlyPerfectAsync(),
                    "0 10 1 * *",
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
