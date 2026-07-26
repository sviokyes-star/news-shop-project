CREATE TABLE IF NOT EXISTS t_p15345778_news_shop_project.purchases (
    id SERIAL PRIMARY KEY,
    steam_id VARCHAR(255) NOT NULL,
    persona_name VARCHAR(255) DEFAULT '',
    product_id INTEGER NOT NULL,
    product_name VARCHAR(255) NOT NULL DEFAULT '',
    amount VARCHAR(100) NOT NULL DEFAULT '',
    price INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_purchases_steam
    ON t_p15345778_news_shop_project.purchases (steam_id);