using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace ochweb.Controllers
{
    [Route("pay")]
    [ApiController] // ✅ 用於 Swagger 支援
    public class EcpayLinePayController : ControllerBase
    {
        private const string MerchantID = "2000132";
        private const string HashKey = "5294y06JbISpM5x9";
        private const string HashIV = "v77hoKGq4kWxNNIS";

        [HttpPost("linepay")]
        public IActionResult LinePay()
        {
            var orderId = "ORDER" + DateTime.Now.ToString("yyyyMMddHHmmss");
            var formHtml = GenerateEcpayForm(orderId, 300, "奉獻支持", "https://workgit.onrender.com/pay/callback", "https://workgit.onrender.com/pay/thankyou");
            return Content(formHtml, "text/html");
        }


        [HttpGet("thankyou")]
        public ContentResult ThankYou()
        {
            var html = @"<html><head><title>付款成功</title></head>
            <body style='font-family:sans-serif;text-align:center;margin-top:50px;'>
                <h2>🎉 付款成功</h2>
                <p>感謝您的支持與奉獻！</p>
                <a href='/' style='text-decoration:none;color:#007bff;'>返回首頁</a>
            </body></html>";
            return Content(html, "text/html");
        }

        [HttpPost("callback")]
        public IActionResult Callback()
        {
            var form = Request.Form;
            string tradeNo = form["TradeNo"];
            string merchantTradeNo = form["MerchantTradeNo"];
            string tradeAmt = form["TradeAmt"];
            string rtnCode = form["RtnCode"];

            if (rtnCode == "1")
            {
                // ✅ 寫入訂單成功邏輯
            }

            return Content("1|OK");
        }

        private string GenerateEcpayForm(string orderId, int amount, string itemName, string returnUrl, string backUrl)
        {
            var dict = new Dictionary<string, string>
            {
                { "MerchantID", MerchantID },
                { "MerchantTradeNo", orderId },
                { "MerchantTradeDate", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") },
                { "PaymentType", "aio" },
                { "TotalAmount", amount.ToString() },
                { "TradeDesc", "LINEPay付款" },
                { "ItemName", itemName },
                { "ReturnURL", returnUrl },
                { "ClientBackURL", backUrl },
                { "ChoosePayment", "LINEPay" },
                { "EncryptType", "1" }
            };

            dict.Add("CheckMacValue", GetCheckMacValue(dict));

            var sb = new StringBuilder();
            sb.AppendLine("<form id='ecpayForm' method='POST' action='https://payment.ecpay.com.tw/Cashier/AioCheckOut/V5'>");
            foreach (var kv in dict)
            {
                sb.AppendLine($"<input type='hidden' name='{kv.Key}' value='{kv.Value}' />");
            }
            sb.AppendLine("</form>");
            sb.AppendLine("<script>document.getElementById('ecpayForm').submit();</script>");
            return sb.ToString();
        }

        private string GetCheckMacValue(Dictionary<string, string> dict)
        {
            var sorted = dict.OrderBy(x => x.Key).ToList();
            var raw = $"HashKey={HashKey}&{string.Join("&", sorted.Select(kv => $"{kv.Key}={kv.Value}"))}&HashIV={HashIV}";

            raw = HttpUtility.UrlEncode(raw).ToLower();
            raw = raw.Replace("%21", "!").Replace("%28", "(").Replace("%29", ")")
                     .Replace("%2a", "*").Replace("%2d", "-").Replace("%2e", ".");

            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(raw));
                return BitConverter.ToString(bytes).Replace("-", "").ToUpper();
            }
        }
    }
}
