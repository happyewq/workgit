using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Net.Http.Headers;

namespace ochweb.ApiController
{
    public class LineWebhookController : Controller
    {
        [HttpPost]
        [Route("line/webhook")]
        public async Task<IActionResult> Post([FromBody] JsonElement json)
        {
            var events = json.GetProperty("events");
            foreach (var ev in events.EnumerateArray())
            {
                var type = ev.GetProperty("type").GetString();
                if (type == "message")
                {
                    var userId = ev.GetProperty("source").GetProperty("userId").GetString();
                    var message = ev.GetProperty("message").GetProperty("text").GetString();
                    var replyToken = ev.GetProperty("replyToken").GetString();

                    Console.WriteLine($"收到 {userId} 說：{message}");

                    // 自己用 HttpClient 呼叫 LINE Messaging API 回覆訊息
                    await ReplyToLineUser(replyToken, "你說的是：" + message);
                }
            }
            return Ok();
        }

        private async Task ReplyToLineUser(string replyToken, string message)
        {
            var httpClient = new HttpClient();
            string channelAccessToken = "6ZhfXTSOnByDZm+3YwzmqIa8oTI9XwVm6sUuhQYZ/QsMa2dLpIODQ1z0RJsquPrUoLImy7rU/qwHbsNsXjfeiDSv0mAcRHkpswjuzDI1Er1GdhCjd1Qg24vghxSWzFA7nbZxHB8AoO1Gm7qvl6AW1wdB04t89/1O/w1cDnyilFU=";

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", channelAccessToken);

            var payload = new
            {
                replyToken = replyToken,
                messages = new[]
                {
                new { type = "text", text = message }
            }
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("https://api.line.me/v2/bot/message/reply", content);
            var result = await response.Content.ReadAsStringAsync();
            Console.WriteLine(result);
        }
    }
}
