CREATE TABLE OchUser (
    UserID         VARCHAR(50) PRIMARY KEY,
    UserNMC        VARCHAR(100) NOT NULL,
    Phone          VARCHAR(20),
    Password       VARCHAR(100) NOT NULL,
    Permission     VARCHAR(50),
    CreateDateTime CHAR(14) DEFAULT TO_CHAR(NOW(), 'YYYYMMDDHH24MISS'),  -- 14碼字串
    ErrorMessage   TEXT
);