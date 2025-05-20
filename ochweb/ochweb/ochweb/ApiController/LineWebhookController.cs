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
            Console.WriteLine("📥 收到 LINE Webhook：");
            Console.WriteLine(json.ToString());

            if (!json.TryGetProperty("events", out var events))
            {
                Console.WriteLine("⚠️ 無 events 陣列，Webhook 格式錯誤！");
                return BadRequest();
            }

            string connstring = DBHelper.GetConnectionString();

            foreach (var ev in events.EnumerateArray())
            {
                if (!ev.TryGetProperty("type", out var typeProp))
                {
                    Console.WriteLine("⚠️ 缺少 type 屬性，跳過");
                    continue;
                }

                var type = typeProp.GetString();
                Console.WriteLine($"🔍 處理事件類型：{type}");

                if (!ev.TryGetProperty("source", out var source) ||
                    !source.TryGetProperty("userId", out var userIdProp))
                {
                    Console.WriteLine("⚠️ 缺少 userId，跳過");
                    continue;
                }

                var userId = userIdProp.GetString();
                var replyToken = ev.TryGetProperty("replyToken", out var rt) ? rt.GetString() : null;
                var displayName = await GetDisplayNameAsync(userId);

                if (type == "follow" && replyToken != null)
                {
                    Console.WriteLine($"✅ 使用者加入好友：{userId} ({displayName})");

                    using var conn = new NpgsqlConnection(connstring);
                    await conn.OpenAsync();
                    await SaveMessageToDb(userId, "加入好友", displayName, conn);
                    await ReplyToLineUser(replyToken, $"👋 歡迎 {displayName} 加入我們的 LINE！您可以輸入「報名」參加活動～");
                    continue;
                }

                if (type == "message")
                {
                    Console.WriteLine("💬 處理文字訊息事件");

                    if (!ev.TryGetProperty("message", out var msgObj) ||
                        !msgObj.TryGetProperty("text", out var textProp))
                    {
                        Console.WriteLine("⚠️ 缺少 message.text");
                        continue;
                    }

                    var message = textProp.GetString();
                    string returnMessage;

                    using var conn = new NpgsqlConnection(connstring);
                    await conn.OpenAsync();

                    string sql = @"SELECT 1 FROM ""OCHUSER"".""linemessages"" WHERE ""UserID"" = @UserID";
                    using var cmd = new NpgsqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@UserID", userId);

                    var isKnownUser = await cmd.ExecuteScalarAsync() != null;

                    if (isKnownUser)
                    {
                        Console.WriteLine($"👤 已知使用者：{userId} 傳來：「{message}」");

                        if (message == "報名")
                        {
                            await INSERTOchregist(userId, displayName, conn);
                            returnMessage = $"🎉 恭喜 {displayName}，您已成功完成報名！請於2025/5/10之前完成繳費！";
                        }
                        else if (message == "繳費")
                        {
                            returnMessage = $"🎉 恭喜 {displayName}，繳費完成！我們期待與您見面！";
                        }
                        else
                        {
                            returnMessage = $"📩 您輸入的是：「{message}」\n若要參加活動，請回覆「報名」兩字。";
                        }
                    }
                    else
                    {
                        Console.WriteLine($"👤 首次使用者：{userId} 訊息：「{message}」");
                        await SaveMessageToDb(userId, message, displayName, conn);
                        await INSERTOchregist(userId, displayName, conn);
                        returnMessage = $"👋 嗨 {displayName}，我們已為您建立資料並完成報名！";
                    }

                    if (replyToken != null)
                    {
                        Console.WriteLine($"📤 回覆訊息給 {userId}");
                        await ReplyToLineUser(replyToken, returnMessage);
                    }
                }
            }

            return Ok();
        }

        private async Task INSERTOchregist(string userId, string displayName, NpgsqlConnection conn)
        {
            string sql = @"INSERT INTO ""OCHUSER"".""ochregist"" 
                        (""UserID"", ""UserNMC"", ""UserType"", ""PaidYN"", ""CancelYN"", ""SessionID"", ""RegisterTime"") 
                        VALUES (@UserID, @UserNMC, @UserType, @PaidYN, @CancelYN, @SessionID, @RegisterTime)";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@UserID", userId);
            cmd.Parameters.AddWithValue("@UserNMC", displayName);
            cmd.Parameters.AddWithValue("@UserType", "w");
            cmd.Parameters.AddWithValue("@PaidYN", "N");
            cmd.Parameters.AddWithValue("@CancelYN", "N");
            cmd.Parameters.AddWithValue("@SessionID", 3);
            cmd.Parameters.AddWithValue("@RegisterTime", DateTime.Now);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task SaveMessageToDb(string userId, string message, string name, NpgsqlConnection conn)
        {
            string sql = @"INSERT INTO ""OCHUSER"".""linemessages"" 
                        (""UserID"", ""Message"", ""UserName"") 
                        VALUES (@UserID, @Message, @UserName)";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@UserID", userId);
            cmd.Parameters.AddWithValue("@Message", message);
            cmd.Parameters.AddWithValue("@UserName", name);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<string> GetDisplayNameAsync(string userId)
        {
            var token = _config["LineBot:ChannelAccessToken"];
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"https://api.line.me/v2/bot/profile/{userId}");
            var content = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(content);
            return doc.RootElement.GetProperty("displayName").GetString();
        }

        private async Task ReplyToLineUser(string replyToken, string message)
        {
            using var httpClient = new HttpClient();
            string channelAccessToken = _config["LineBot:ChannelAccessToken"];
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", channelAccessToken);

            var payload = new
            {
                replyToken,
                messages = new[] { new { type = "text", text = message } }
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("https://api.line.me/v2/bot/message/reply", content);
            var result = await response.Content.ReadAsStringAsync();
            Console.WriteLine(result);
        }
    }
}


// https://workgit.onrender.com/line/webhook
// https://hook.eu2.make.com/1obevqa6h6d3ne5hef4zpadrv4d5wbhv


//SELECT pid, usename, client_addr, state
//FROM pg_stat_activity
//WHERE datname = 'ochdb'
//  AND pid <> pg_backend_pid(); -- 🔒 排除自己

//SELECT pg_terminate_backend(pid)
//FROM pg_stat_activity
//WHERE client_addr = '10.223.148.33'; --指定 IP