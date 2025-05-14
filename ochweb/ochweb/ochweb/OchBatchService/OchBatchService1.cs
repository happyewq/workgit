using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace ochweb.OchBatchService
{
    public class OchBatchService1
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task SendLine()
        {
            string userID = "Ue2422631cd76bfdebd2249811a1d2de6";
            string channelAccessToken = "sfw8nHDe12BGGoWpUobiL/P5j/dWl7HDWbQPxrfptaR3pApp0ZR2FO2ovpOVxB79LdJl9Nhy6qN8p9D2BHqaxMtQLUbFEY95IfvIpCIm/TuebEy4HCH7OmVjFV/xKnN4ReocVChKkobNcpNzWFjVhgdB04t89/1O/w1cDnyilFU=";
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
