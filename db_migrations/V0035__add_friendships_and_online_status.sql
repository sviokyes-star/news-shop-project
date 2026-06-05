CREATE TABLE t_p15345778_news_shop_project.friendships (
    id SERIAL PRIMARY KEY,
    requester_steam_id VARCHAR(50) NOT NULL,
    addressee_steam_id VARCHAR(50) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'pending',
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    UNIQUE(requester_steam_id, addressee_steam_id)
);

ALTER TABLE t_p15345778_news_shop_project.users
    ADD COLUMN IF NOT EXISTS last_online TIMESTAMP DEFAULT NOW(),
    ADD COLUMN IF NOT EXISTS is_online BOOLEAN DEFAULT FALSE;

CREATE INDEX idx_friendships_requester ON t_p15345778_news_shop_project.friendships(requester_steam_id);
CREATE INDEX idx_friendships_addressee ON t_p15345778_news_shop_project.friendships(addressee_steam_id);
