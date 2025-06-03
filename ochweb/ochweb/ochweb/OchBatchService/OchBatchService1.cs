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

        public async Task SendUnReadYesterdayAsync1()
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
            await SendToGroup1(message);
        }

        private async Task SendToGroup1(string message)
        {
            // Token A: 預設帳號
            string token1 = _config["LineBot:ChannelAccessToken1"];
            // Token B: 備用帳號（第二個 Bot）
            string token2 = _config["LineBot:ChannelAccessToken1"];

            // 嘗試使用兩個 Token 依序發送
            var tokens = new[] { token1, token2 };

            foreach (var token in tokens)
            {
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
                    Console.WriteLine($"✅ 群組推播成功！（使用 Token：{token}）");
                    return; // 成功就不再嘗試其他 token
                }

                Console.WriteLine($"❌ 使用 Token 失敗：{token}");
                Console.WriteLine(result);

                // 如果是額度用盡，就嘗試下個 token
                if (!result.Contains("You have reached your monthly limit"))
                {
                    // 若是其他錯誤，不再嘗試
                    break;
                }
            }

            Console.WriteLine("⛔ 所有帳號推播皆失敗！");
        }


        private async Task SendToGroup(string message)
        {
            // Token A: 預設帳號
            string token1 = _config["LineBot:ChannelAccessToken"];
            // Token B: 備用帳號（第二個 Bot）
            string token2 = _config["LineBot:ChannelAccessToken1"];

            // 嘗試使用兩個 Token 依序發送
            var tokens = new[] { token1, token2 };

            foreach (var token in tokens)
            {
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
                    Console.WriteLine($"✅ 群組推播成功！（使用 Token：{token}）");
                    return; // 成功就不再嘗試其他 token
                }

                Console.WriteLine($"❌ 使用 Token 失敗：{token}");
                Console.WriteLine(result);

                // 如果是額度用盡，就嘗試下個 token
                if (!result.Contains("You have reached your monthly limit"))
                {
                    // 若是其他錯誤，不再嘗試
                    break;
                }
            }

            Console.WriteLine("⛔ 所有帳號推播皆失敗！");
        }


    }
}
