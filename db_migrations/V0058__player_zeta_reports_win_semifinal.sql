UPDATE t_p15345778_news_shop_project.match_lobbies
SET player2_reported_winner = '76561190000000006',
    updated_at = NOW()
WHERE id = 4;

INSERT INTO t_p15345778_news_shop_project.lobby_messages (lobby_id, steam_id, persona_name, avatar_url, message)
VALUES (4, '76561190000000006', 'Player_Zeta', 'https://avatars.steamstatic.com/fef49e7fa7e1997310d705b2a6158ff8dc1cdfeb_full.jpg', 'Я победил! Скриншот готов.');