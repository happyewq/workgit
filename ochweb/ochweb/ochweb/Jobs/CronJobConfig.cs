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
            RecurringJob.AddOrUpdate(() => OchBatchService1.SendLine(), "0 0 1 * *", TimeZoneInfo.Local);
        }
    }
}
