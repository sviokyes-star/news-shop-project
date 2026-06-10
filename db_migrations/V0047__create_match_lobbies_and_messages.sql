CREATE TABLE IF NOT EXISTS t_p15345778_news_shop_project.match_lobbies (
    id SERIAL PRIMARY KEY,
    tournament_id INTEGER NOT NULL,
    round_index INTEGER NOT NULL,
    match_index INTEGER NOT NULL,
    player1_steam_id VARCHAR(255),
    player2_steam_id VARCHAR(255),
    winner_steam_id VARCHAR(255),
    status VARCHAR(20) NOT NULL DEFAULT 'waiting',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(tournament_id, round_index, match_index)
);

CREATE TABLE IF NOT EXISTS t_p15345778_news_shop_project.lobby_messages (
    id SERIAL PRIMARY KEY,
    lobby_id INTEGER NOT NULL,
    steam_id VARCHAR(255) NOT NULL,
    persona_name VARCHAR(255) NOT NULL,
    avatar_url VARCHAR(500),
    message TEXT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);