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
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;

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

                var replyToken = ev.TryGetProperty("replyToken", out var rt) ? rt.GetString() : null;

                if (!ev.TryGetProperty("source", out var source))
                {
                    Console.WriteLine("⚠️ 缺少 source，跳過");
                    continue;
                }

                if (type == "join" && replyToken != null)
                {
                    var groupId = source.GetProperty("groupId").GetString();
                    Console.WriteLine($"✅ Bot 加入群組，GroupId：{groupId}");
                    await ReplyToLineUser(replyToken, "👋 我加入群組囉，有需要可以打『請發』來呼叫我！");
                    continue;
                }

                if (type == "leave")
                {
                    var groupId = source.GetProperty("groupId").GetString();
                    Console.WriteLine($"⚠️ Bot 被移出群組，GroupId：{groupId}");
                    continue;
                }

                if (type == "memberJoined")
                {
                    var newUserId = ev.GetProperty("joined").GetProperty("members")[0].GetProperty("userId").GetString();
                    Console.WriteLine($"👤 有人加入群組：{newUserId}");
                    continue;
                }

                if (type == "memberLeft")
                {
                    var leftUserId = ev.GetProperty("left").GetProperty("members")[0].GetProperty("userId").GetString();
                    Console.WriteLine($"👤 有人離開群組：{leftUserId}");
                    continue;
                }

                if (!source.TryGetProperty("userId", out var userIdProp))
                {
                    Console.WriteLine("⚠️ 缺少 userId，跳過");
                    continue;
                }

                var userId = userIdProp.GetString();
                var displayName = await GetDisplayNameAsync(userId);

                if (type == "follow" && replyToken != null)
                {
                    Console.WriteLine($"✅ 使用者加入好友：{userId} ({displayName})");

                    using var conn = new NpgsqlConnection(connstring);
                    await conn.OpenAsync();
                    await SaveMessageToDb(userId, "加入好友", displayName, conn);
                    await ReplyToLineUser(replyToken, $"👋 歡迎 {displayName} 加入天天讀經的行列! 神的話語必成為你腳前的燈和路上的光");
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

                    // ✔️ 指定人 + 指定訊息 + 指定群組觸發未讀經清單推播
                    if (message.Trim() == "請發"
                        && userId == "Ue2422631cd76bfdebd2249811a1d2de6"
                        && source.GetProperty("type").GetString() == "group"
                        && source.TryGetProperty("groupId", out var gidProp)
                        && gidProp.GetString() == "Cf1cf1bb73a1980f358a7341b932c4f76")
                    {
                        Console.WriteLine("🎯 指定人員在群組下遞『請發』命令，準備推播未讀清單");
                        string message1 = await GetUnreadMessageAsync(); // 🆕 我幫你新增的只組訊息的方法
                        await ReplyToLineUser(replyToken, message1); // 🆗 直接回覆到該群組
                    }
                    // 👈 新增的 groupId 記錄段
                    //if (source.TryGetProperty("type", out var sourceTypeProp) &&
                    //    sourceTypeProp.GetString() == "group" &&
                    //    source.TryGetProperty("groupId", out var groupIdProp))
                    //{
                    //    var groupId = groupIdProp.GetString();
                    //    Console.WriteLine($"👥 事件來自群組：{groupId}");
                    //}
                    // ✅ 判斷是否為群組且包含聖經書名
                    if (source.GetProperty("type").GetString() == "group" &&
                        source.TryGetProperty("groupId", out var groupIdProp) &&
                        groupIdProp.GetString() == "Cf1cf1bb73a1980f358a7341b932c4f76")
                    {
                        var detectedBook = DetectBibleBook(message); // 👈 這裡是偵測書名
                        if (detectedBook != null)
                        {
                            Console.WriteLine($"📌 群組發言提到聖經書卷：{detectedBook}，來自 {userId}");
                            await InsertGroupSpeakLog(userId, conn);
                        }
                    }

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

                    if (replyToken != null && source.GetProperty("type").GetString() != "group")
                    {
                        Console.WriteLine($"📤 回覆訊息給 {userId}");
                        //await ReplyToLineUser(replyToken, returnMessage); 暫時不回訊息
                    }
                    else
                    {
                        Console.WriteLine("🤫 來自群組，不回覆訊息");
                    }
                }
            }

            return Ok();
        }

        public async Task<string> GetUnreadMessageAsync()
        {
            string connStr = DBHelper.GetConnectionString();
            string yesterday = DateTime.Today.AddDays(-1).ToString("yyyyMMdd");

            var userMap = new Dictionary<string, string>(); // userId -> userName
            var unreadList = new List<string>();

            using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync();

            // 加入好友者
            var cmdUsers = new NpgsqlCommand(@"
                SELECT DISTINCT ""UserID"", ""UserName""
                FROM ""OCHUSER"".""linemessages""
                WHERE ""Message"" = '加入好友';
            ", conn);
            using (var reader = await cmdUsers.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    userMap[reader.GetString(0)] = reader.GetString(1);
                }
            }

            // 有讀經的人
            // 取得昨天有讀經的人
            var cmdBible = new NpgsqlCommand(@"
        SELECT DISTINCT ""UserID""
        FROM ""OCHUSER"".""ochbible""
        WHERE ""CreateDateTime"" = @date;
    ", conn);
            cmdBible.Parameters.AddWithValue("@date", yesterday);

            var readSet = new HashSet<string>();
            using (var reader = await cmdBible.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    readSet.Add(reader.GetString(0));
                }
            }

            foreach (var kv in userMap)
            {
                if (!readSet.Contains(kv.Key))
                {
                    unreadList.Add(kv.Value);
                }
            }

            string message;
            if (unreadList.Count == 0)
            {
                message = $"✅ 昨日 ({yesterday}) 全員皆有讀經，感謝主！";
            }
            else
            {
                var nameList = string.Join("\\n", unreadList.Select(n => $"❌ {n}"));
                message = $"📋 昨日未讀經清單（{yesterday}）共 {unreadList.Count} 人：\n{nameList}\n\n📖 繼續加油！讓祂的話語成為你腳前的燈、路上的光。";
            }

            return message;
        }


        private string? DetectBibleBook(string message)
        {
            foreach (var book in BibleBooks)
            {
                if (message.Contains(book))
                {
                    return book;
                }
            }
            return null;
        }

        private static readonly string[] BibleBooks = new[]
        {
            // 舊約 39 卷
            "創世紀", "出埃及記", "利未記", "民數記", "申命記",
            "約書亞記", "士師記", "路得記",
            "撒母耳記上", "撒母耳記下", "列王記上", "列王記下",
            "歷代志上", "歷代志下", "以斯拉記", "尼希米記", "以斯帖記",
            "約伯記", "詩篇", "箴言", "傳道書", "雅歌",
            "以賽亞書", "耶利米書", "耶利米哀歌", "以西結書", "但以理書",
            "何西阿書", "約珥書", "阿摩司書", "俄巴底亞書", "約拿書",
            "彌迦書", "那鴻書", "哈巴谷書", "西番雅書", "哈該書", "撒迦利亞書", "瑪拉基書",
        
            // 新約 27 卷
            "馬太福音", "馬可福音", "路加福音", "約翰福音", "使徒行傳",
            "羅馬書", "哥林多前書", "哥林多後書", "加拉太書",
            "以弗所書", "腓立比書", "歌羅西書",
            "帖撒羅尼迦前書", "帖撒羅尼迦後書",
            "提摩太前書", "提摩太後書", "提多書", "腓利門書",
            "希伯來書", "雅各書", "彼得前書", "彼得後書",
            "約翰一書", "約翰二書", "約翰三書", "猶大書", "啟示錄"
        };

        private async Task InsertGroupSpeakLog(string userId, NpgsqlConnection conn)
        {


            string tzId = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "Taipei Standard Time"      // Windows 時區 ID
                : "Asia/Taipei";              // Linux/macOS 時區 ID

            TimeZoneInfo taiwanTimeZone = TimeZoneInfo.FindSystemTimeZoneById(tzId);
            DateTime taiwanNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, taiwanTimeZone);
            string dateText = taiwanNow.ToString("yyyyMMdd");
            string dateTextlog = taiwanNow.ToString("yyyyMMddHHmmss");
            Console.WriteLine($"📌 {dateTextlog} 紀錄時間");

            // ✅ 先檢查是否已存在今天的紀錄
            string checkSql = @"SELECT 1 FROM ""OCHUSER"".""ochbible"" 
                        WHERE ""UserID"" = @UserID AND ""CreateDateTime"" = @CreateDateTime";
            using (var checkCmd = new NpgsqlCommand(checkSql, conn))
            {
                checkCmd.Parameters.AddWithValue("@UserID", userId);
                checkCmd.Parameters.AddWithValue("@CreateDateTime", dateText);

                var exists = await checkCmd.ExecuteScalarAsync();
                if (exists != null)
                {
                    Console.WriteLine($"📌 {userId} 今天已經記錄過，不重複 insert");
                    return;
                }
            }

            // ✅ 若尚未記錄，則插入資料
            string insertSql = @"INSERT INTO ""OCHUSER"".""ochbible"" (""UserID"", ""CreateDateTime"") 
                         VALUES (@UserID, @CreateDateTime)";
            using var insertCmd = new NpgsqlCommand(insertSql, conn);
            insertCmd.Parameters.AddWithValue("@UserID", userId);
            insertCmd.Parameters.AddWithValue("@CreateDateTime", dateText);
            await insertCmd.ExecuteNonQueryAsync();

            Console.WriteLine($"✅ 已新增讀經紀錄：{userId} - {dateText}");
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