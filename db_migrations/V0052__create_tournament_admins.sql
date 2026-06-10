CREATE TABLE IF NOT EXISTS t_p15345778_news_shop_project.tournament_admins (
    id SERIAL PRIMARY KEY,
    tournament_id INTEGER NOT NULL,
    steam_id VARCHAR(255) NOT NULL,
    persona_name VARCHAR(255) NOT NULL,
    avatar_url VARCHAR(500),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(tournament_id, steam_id)
);