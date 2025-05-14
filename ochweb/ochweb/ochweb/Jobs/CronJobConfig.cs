using Hangfire;
using ochweb.OchBatchService;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;

namespace CcpBatch.Jobs
{
    public class CronJobConfig
    {
        public static void Register(IWebHostEnvironment env, IConfiguration config)
        {
            OchBatchService1 NewCcpBatchService = new OchBatchService1();

                // 如果是線上系統區域，註冊定期任務
                RecurringJob.AddOrUpdate(() => NewCcpBatchService.MergeRegic(), // 執行的方法
                    "0 0 1 * *", // 每個月的1號的午夜執行
                    TimeZoneInfo.Local // 使用本地時間
                );
        }
    }
}
