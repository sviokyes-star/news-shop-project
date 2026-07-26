CREATE TABLE IF NOT EXISTS t_p15345778_news_shop_project.game_deliveries (
    id SERIAL PRIMARY KEY,
    steam_id VARCHAR(255) NOT NULL,
    currency VARCHAR(20) NOT NULL,
    amount INTEGER NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'pending',
    purchase_id INTEGER,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    delivered_at TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_game_deliveries_pending
    ON t_p15345778_news_shop_project.game_deliveries (status)
    WHERE status = 'pending';

CREATE INDEX IF NOT EXISTS idx_game_deliveries_steam
    ON t_p15345778_news_shop_project.game_deliveries (steam_id);