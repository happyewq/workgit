namespace ochweb.Models
{
    // 讀經名單設定：一筆代表一位加入好友的 LINE 使用者
    public class OchC040View
    {
        public string UserID { get; set; }
        public string UserName { get; set; }
        public bool ShowUnread { get; set; } = true;   // 打「發」時是否列在未讀名單
        public string UpdateUser { get; set; }
        public string UpdateDateTime { get; set; }
    }
}
