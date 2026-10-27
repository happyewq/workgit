-- 讀經名單設定：控制在群組打「發」時，這個人要不要出現在未讀名單裡
-- 沒有資料的人預設為 'Y'（照常列出），只有設成 'N' 的人會被排除
CREATE TABLE IF NOT EXISTS "OCHUSER"."lineuser_setting" (
    "UserID"         varchar(64) PRIMARY KEY,           -- LINE UserID，對應 linemessages."UserID"
    "ShowUnreadYN"   char(1)     NOT NULL DEFAULT 'Y',  -- Y = 列出、N = 不列出
    "UpdateUser"     varchar(50),                       -- 最後修改的後台帳號
    "UpdateDateTime" timestamp   NOT NULL DEFAULT now()
);
