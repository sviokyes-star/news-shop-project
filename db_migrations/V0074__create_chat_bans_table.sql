CREATE TABLE t_p15345778_news_shop_project.chat_bans (
    id SERIAL PRIMARY KEY,
    steam_id VARCHAR(255) NOT NULL,
    banned_by VARCHAR(255) NOT NULL,
    reason VARCHAR(255) NOT NULL DEFAULT 'нарушение правил',
    expires_at TIMESTAMP NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX idx_chat_bans_steam_id ON t_p15345778_news_shop_project.chat_bans(steam_id);