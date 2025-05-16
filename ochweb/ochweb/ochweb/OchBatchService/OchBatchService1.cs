using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace ochweb.OchBatchService
{
    public class OchBatchService1
    {
        private static readonly HttpClient httpClient = new HttpClient();

        private readonly IConfiguration _config;

        public OchBatchService1(IConfiguration config)
        {
            _config = config;
        }
        public async Task SendLine()
        {
            string userID = "Ue2422631cd76bfdebd2249811a1d2de6";
            string channelAccessToken = _config["LineBot:ChannelAccessToken"];
            string message = "請繳費";

            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", channelAccessToken);

            var payload = new
            {
                to = userID,
                messages = new[]
                {
            new { type = "text", text = message }
        }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync("https://api.line.me/v2/bot/message/push", content);
                var resultContent = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"✅ 推播狀態：{response.StatusCode}");
                Console.WriteLine($"📄 回傳內容：{resultContent}");

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("❌ 推播失敗：" + resultContent);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("⛔ 發送失敗：" + ex.Message);
                // 可考慮寫 log 或丟出給 Hangfire Retry
                throw;
            }
        }


    }
}
