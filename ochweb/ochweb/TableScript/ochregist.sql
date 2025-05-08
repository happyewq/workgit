CREATE TABLE OCHRegist (
    UserID         VARCHAR(50) NOT NULL,                -- 報名者 ID（可連結 User 表）
    UserType       VARCHAR(20) NOT NULL,                -- 報名者身分（學生 / 上班族） 
    --w :上班
    --s :學生
    FeeAmount      NUMERIC(10, 2) NOT NULL,             -- 應繳費用
    PaidYN         CHAR(1) NOT NULL,               -- 是否已繳交（預設 false）
    CancelYN       CHAR(1) NOT NULL,                    -- 是否取消報名（Y/N）
    SessionID      INT NOT NULL,                        -- 報名場次 ID（可連結 Session 表）
    RegisterTime   TIMESTAMP DEFAULT CURRENT_TIMESTAMP  -- 報名時間
);