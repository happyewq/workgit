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

                    var displayName = await GetDisplayNameAsync(userId);
                    var connstring = DBHelper.GetConnectionString();
                    string returnMessage;

                    using (var conn = new NpgsqlConnection(connstring))
                    {
                        conn.Open();
                        string sql = @"SELECT * FROM ""OCHUSER"".""linemessages"" WHERE ""UserID"" = @UserID";

                        using (var cmd = new NpgsqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@UserID", userId);

                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    // ✅ 已存在於 linemessages 表
                                    if (message == "報名")
                                    {
                                        INSERTOchregist(userId);
                                        returnMessage = $"🎉 恭喜 {displayName}，您已成功完成報名！我們期待與您見面！";
                                    }
                                    else
                                    {
                                        returnMessage = $"📩 您輸入的是：「{message}」\n若要參加活動，請回覆「報名」兩字。";
                                    }
                                }
                                else
                                {
                                    // ✅ 沒有資料，先新增 linemessages 記錄與註冊
                                    SaveMessageToDb(userId, message, displayName);
                                    INSERTOchregist(userId);
                                    returnMessage = $"👋 嗨 {displayName}，我們已為您建立資料並完成報名！";
                                }
                            }
                        }
                    }

                    // ✅ 使用正確訊息回覆
                    await ReplyToLineUser(replyToken, returnMessage);
                }
            }

            return Ok();
        }

        private void INSERTOchregist(string userId)
        {
            string connstring = DBHelper.GetConnectionString(); // 從 appsettings.json 抓
            using (var conn = new NpgsqlConnection(connstring))
            {
                conn.Open();
                string sql = @"INSERT INTO ""OCHUSER"".""ochregist"" 
                       (""UserID"", ""UserType"", ""PaidYN"",""CancelYN"", ""SessionID"", ""RegisterTime"") 
                       VALUES (@UserID, @UserType, @PaidYN, @CancelYN, @SessionID, @RegisterTime)";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@UserType", "w");
                    cmd.Parameters.AddWithValue("@PaidYN", "N");
                    cmd.Parameters.AddWithValue("@CancelYN", "N");
                    cmd.Parameters.AddWithValue("@SessionID", 3);
                    cmd.Parameters.AddWithValue("@RegisterTime", DateTime.Now);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void SaveMessageToDb(string userId, string message, string name)
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
