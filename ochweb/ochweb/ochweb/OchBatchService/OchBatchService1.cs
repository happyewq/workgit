using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System;
using System.Text.Json;

namespace ochweb.OchBatchService
{
    public class OchBatchService1
    {
        public static void SendLine()
        {
            string userID = "Ue2422631cd76bfdebd2249811a1d2de6";
            string channelAccessToken = "sfw8nHDe12BGGoWpUobiL/P5j/dWl7HDWbQPxrfptaR3pApp0ZR2FO2ovpOVxB79LdJl9Nhy6qN8p9D2BHqaxMtQLUbFEY95IfvIpCIm/TuebEy4HCH7OmVjFV/xKnN4ReocVChKkobNcpNzWFjVhgdB04t89/1O/w1cDnyilFU=";
            string Message = "請繳費";

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", channelAccessToken);

                var payload = new
                {
                    to = userID,
                    messages = new[]
                    {
                new
                {
                    type = "text",
                    text = Message
                }
            }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = httpClient.PostAsync("https://api.line.me/v2/bot/message/push", content);
                var result = response;

                Console.WriteLine($"推播結果：{result}");
            }
        }
    }
}
