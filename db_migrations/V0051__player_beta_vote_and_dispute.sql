-- Player_Beta голосует за себя
UPDATE t_p15345778_news_shop_project.match_lobbies
SET player2_reported_winner = '76561190000000002',
    is_dispute = TRUE,
    status = 'dispute',
    updated_at = NOW()
WHERE id = 1;

-- Player_Beta пишет в чат
INSERT INTO t_p15345778_news_shop_project.lobby_messages (lobby_id, steam_id, persona_name, avatar_url, message)
VALUES (1, '76561190000000002', 'Player_Beta', 'https://avatars.steamstatic.com/fef49e7fa7e1997310d705b2a6158ff8dc1cdfeb_full.jpg', 'Я выиграл этот матч, победа за мной!');

-- Системное сообщение о споре
INSERT INTO t_p15345778_news_shop_project.lobby_messages (lobby_id, steam_id, persona_name, message)
VALUES (1, 'system', 'Система', '⚠️ Игроки указали разных победителей. Открыт спор — ожидается решение администратора.');