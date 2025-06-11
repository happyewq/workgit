using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Npgsql;
using ochweb.Helpers;
using System.Collections.Generic;
using System.Linq;
using Hangfire;

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

        public async Task SendUnReadYesterdayAsync()
        {
            string connStr = DBHelper.GetConnectionString();
            string yesterday = DateTime.Today.AddDays(-1).ToString("yyyyMMdd");

            var userMap = new Dictionary<string, string>(); // userId -> userName
            var unreadList = new List<string>();

            using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync();

            // 取得所有加入好友者
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

            // 找出沒讀的
            foreach (var kv in userMap)
            {
                if (!readSet.Contains(kv.Key))
                {
                    unreadList.Add(kv.Value);
                }
            }

            // 組訊息
            string message;
            if (unreadList.Count == 0)
            {
                message = $"✅ 昨日 ({yesterday}) 全員皆有讀經，感謝主！";
            }
            else
            {
                var nameList = string.Join("\n", unreadList.Select(n => $"❌ {n}"));
                message = $"📋 昨日未讀經清單（{yesterday}）共 {unreadList.Count} 人：\n{nameList}\n\n📖 繼續加油！讓祂的話語成為你腳前的燈、路上的光。";
            }

            // 傳送到群組
            await SendToGroup(message);
        }

        /// <summary>
        /// 三天未讀經
        /// </summary>
        /// <returns></returns>
        [AutomaticRetry(Attempts = 0)]
        public async Task SendUnReadThreeDaysAsync()
        {
            Console.WriteLine($"近來 SendUnReadThreeDaysAsync");

            try
            {
                string connStr = DBHelper.GetConnectionString();
                var today = DateTime.Today;
                var dates = new List<string>
        {
            today.AddDays(-1).ToString("yyyyMMdd"),
            today.AddDays(-2).ToString("yyyyMMdd"),
            today.AddDays(-3).ToString("yyyyMMdd")
        };

                var userMap = new Dictionary<string, string>(); // userId -> userName
                var readUserSet = new HashSet<string>(); // 三天內有讀經的人
                var unreadUserList = new List<string>(); // 未讀者 UserID 清單

                using var conn = new NpgsqlConnection(connStr);
                await conn.OpenAsync();

                // 所有加入好友的使用者
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

                // 三天內有讀經的使用者
                var cmdBible = new NpgsqlCommand(@"
            SELECT DISTINCT ""UserID""
            FROM ""OCHUSER"".""ochbible""
            WHERE ""CreateDateTime"" = ANY(@dates);
        ", conn);
                cmdBible.Parameters.AddWithValue("@dates", dates);

                using (var reader = await cmdBible.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        readUserSet.Add(reader.GetString(0));
                    }
                }

                // 找出三天都沒讀經的使用者
                foreach (var kv in userMap)
                {
                    if (!readUserSet.Contains(kv.Key))
                    {
                        unreadUserList.Add(kv.Key);
                    }
                }

                var scriptures = new List<string>
        {
            "以賽亞書 41:10\n「你不要害怕，因為我與你同在；不要驚惶，因為我是你的　神。我必堅固你，我必幫助你，我必用我公義的右手扶持你。」",
            "耶利米書 29:11\n「耶和華說：我知道我向你們所懷的意念，是賜平安的意念，不是降災禍的意念，要叫你們末後有指望。」",
            "腓立比書 4:13\n「我靠著那加給我力量的，凡事都能做。」",
            "詩篇 46:1\n「神是我們的避難所，是我們的力量，是我們在患難中隨時的幫助。」",
            "馬太福音 11:28\n「凡勞苦擔重擔的人可以到我這裡來，我就使你們得安息。」",
            "約書亞記 1:9\n「我豈沒有吩咐你嗎？你當剛強壯膽！不要懼怕，也不要驚惶，因為你無論往哪裡去，耶和華你的神必與你同在。」",
            "羅馬書 8:28\n「我們曉得萬事都互相效力，叫愛神的人得益處，就是按他旨意被召的人。」",
            "詩篇 34:18\n「耶和華靠近傷心的人，拯救靈性痛悔的人。」",
            "哥林多後書 12:9\n「他對我說：『我的恩典夠你用的，因為我的能力是在人的軟弱上顯得完全。』」",
            "箴言 3:5-6\n「你要專心仰賴耶和華，不可倚靠自己的聰明。在你一切所行的事上都要認定他，他必指引你的路。」"
        };

                // 傳送提醒訊息（目前只測試一人）
                string dateRange = $"{dates[2]} ~ {dates[0]}";
                var random = new Random();

                var userId = "Ue2422631cd76bfdebd2249811a1d2de6";
                if (userMap.ContainsKey(userId))
                {
                    var userName = userMap[userId];
                    string verse = scriptures[random.Next(scriptures.Count)];

                    string message = $"愛主的 {userName}，你在 {dateRange} 這三天內沒有讀經紀錄 📖\n\n" +
                                     "鼓勵你天天親近主，祂的話語是我們生命的糧！加油 💪\n\n" +
                                     $"📖 今日經文：\n{verse}";

                    try
                    {
                        Console.WriteLine($"✅ 準備傳送給 {userName}");
                        await SendToUser(userId, message);
                        Console.WriteLine($"✅ 傳送成功給 {userName}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❗ 傳送給 {userName} 失敗: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"⚠️ 找不到 userId：{userId} 對應的名稱");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❗ 整體任務失敗：{ex.Message}");
            }
        }



        public async Task SendToUser(string userId, string message)
        {
            var channelAccessToken = _config["LineBot:ChannelAccessToken"];
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", channelAccessToken);

            var payload = new
            {
                to = userId,
                messages = new[]
                {
            new { type = "text", text = message }
        }
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync("https://api.line.me/v2/bot/message/push", content);
                Console.WriteLine($"📤 [Line Push] 發送給 {userId} - StatusCode: {(int)response.StatusCode} {response.ReasonPhrase}");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❗ [Line Push] 發送失敗內容：{error}");
                }
                else
                {
                    Console.WriteLine("✅ [Line Push] 發送成功");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❗ [Line Push] 發送時發生例外：{ex.Message}");
            }
        }




        private async Task SendToGroup(string message)
        {
            // 使用預設帳號的 Token
            string token = _config["LineBot:ChannelAccessToken"];

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var payload = new
            {
                to = "Cf1cf1bb73a1980f358a7341b932c4f76", // ✅ 群組 ID
                messages = new[] { new { type = "text", text = message } }
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("https://api.line.me/v2/bot/message/push", content);
            var result = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("✅ 群組推播成功！");
            }
            else
            {
                Console.WriteLine("❌ 群組推播失敗！");
                Console.WriteLine(result);
            }
        }


    }
}
