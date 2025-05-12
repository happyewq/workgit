using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Net.Http.Headers;
using Npgsql;
using ochweb.Helpers;
using Microsoft.Extensions.Configuration;

namespace ochweb.ApiController
{
    public class LineWebhookController : Controller
    {
        private readonly IConfiguration _config;

        public LineWebhookController(IConfiguration config)
        {
            _config = config;
        }
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

                    // ✅ 拿使用者的暱稱
                    var displayName = await GetDisplayNameAsync(userId);
                    // ✅ 寫入 PostgreSQL 資料庫
                    SaveMessageToDb(userId, displayName, message);

                    // 回覆
                    await ReplyToLineUser(replyToken, $"哈囉 {displayName}，你說的是：{message}");
                }
            }

            return Ok();
        }

        ////取得他的名稱
        //public async Task<string> GetDisplayNameAsync(string userId)
        //{
        //    var token = "sfw8nHDe12BGGoWpUobiL/P5j/dWl7HDWbQPxrfptaR3pApp0ZR2FO2ovpOVxB79LdJl9Nhy6qN8p9D2BHqaxMtQLUbFEY95IfvIpCIm/TuebEy4HCH7OmVjFV/xKnN4ReocVChKkobNcpNzWFjVhgdB04t89/1O/w1cDnyilFU="; // 注意：要用 Messaging API 的 Token
        //    using var client = new HttpClient();
        //    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        //    var response = await client.GetAsync($"https://api.line.me/v2/bot/profile/{userId}");
        //    var content = await response.Content.ReadAsStringAsync();

        //    using var doc = JsonDocument.Parse(content);
        //    var displayName = doc.RootElement.GetProperty("displayName").GetString();
        //    return displayName;
        //}

        public async Task<string> GetDisplayNameAsync(string userId)
        {
            var token = _config["LineBot:ChannelAccessToken"]; // 從環境變數或 appsettings 讀取
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"https://api.line.me/v2/bot/profile/{userId}");
            var content = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(content);
            var displayName = doc.RootElement.GetProperty("displayName").GetString();
            return displayName;
        }

        private void SaveMessageToDb(string userId, string message,string name)
        {
            string connstring = DBHelper.GetConnectionString(); // 從 appsettings.json 抓
            using (var conn = new NpgsqlConnection(connstring))
            {
                conn.Open();
                string sql = @"INSERT INTO ""OCHUSER"".""linemessages"" (""UserID"", ""Message"",""UserName"") VALUES (@UserID, @Message, @UserName)";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@Message", message);

                    cmd.Parameters.AddWithValue("@UserName", name);
                    cmd.ExecuteNonQuery();
                }
            }
        }


        private async Task ReplyToLineUser(string replyToken, string message)
        {
            var httpClient = new HttpClient();
            //string channelAccessToken = "sfw8nHDe12BGGoWpUobiL/P5j/dWl7HDWbQPxrfptaR3pApp0ZR2FO2ovpOVxB79LdJl9Nhy6qN8p9D2BHqaxMtQLUbFEY95IfvIpCIm/TuebEy4HCH7OmVjFV/xKnN4ReocVChKkobNcpNzWFjVhgdB04t89/1O/w1cDnyilFU=";
            string channelAccessToken = _config["LineBot:ChannelAccessToken"]; // ✅ 從設定讀取
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

//https://workgit.onrender.com/line/webhook
//https://hook.eu2.make.com/1obevqa6h6d3ne5hef4zpadrv4d5wbhv


//阿坤1號channelAccessToken
//6ZhfXTSOnByDZm+3YwzmqIa8oTI9XwVm6sUuhQYZ/QsMa2dLpIODQ1z0RJsquPrUoLImy7rU/qwHbsNsXjfeiDSv0mAcRHkpswjuzDI1Er1GdhCjd1Qg24vghxSWzFA7nbZxHB8AoO1Gm7qvl6AW1wdB04t89/1O/w1cDnyilFU=

//阿坤2號channelAccessToken
//sfw8nHDe12BGGoWpUobiL/P5j/dWl7HDWbQPxrfptaR3pApp0ZR2FO2ovpOVxB79LdJl9Nhy6qN8p9D2BHqaxMtQLUbFEY95IfvIpCIm/TuebEy4HCH7OmVjFV/xKnN4ReocVChKkobNcpNzWFjVhgdB04t89/1O/w1cDnyilFU=
