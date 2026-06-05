CREATE TABLE t_p15345778_news_shop_project.notifications (
    id SERIAL PRIMARY KEY,
    steam_id VARCHAR(50) NOT NULL,
    type VARCHAR(50) NOT NULL,
    title VARCHAR(255) NOT NULL,
    body TEXT,
    link VARCHAR(255),
    is_read BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE INDEX idx_notifications_steam_id ON t_p15345778_news_shop_project.notifications(steam_id);
CREATE INDEX idx_notifications_unread ON t_p15345778_news_shop_project.notifications(steam_id, is_read);
