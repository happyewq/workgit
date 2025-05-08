CREATE TABLE ochSession (
    SessionID        SERIAL PRIMARY KEY,                                 -- 場次 ID（自動流水號）
    SessionName      VARCHAR(100) NOT NULL,                              -- 場次名稱
    SessionContent   TEXT,                                               -- 場次內容
    SessionLocation  VARCHAR(100),                                       -- 場次地點
    StartTime        TIMESTAMP NOT NULL,                                 -- 開始時間
    EndTime          TIMESTAMP NOT NULL,                                 -- 結束時間
    CreateDateTime   CHAR(14) DEFAULT TO_CHAR(NOW(), 'YYYYMMDDHH24MISS') -- 建立時間（14碼）
);