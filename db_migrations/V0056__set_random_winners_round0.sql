-- Матч 0: победитель Player_Gamma (76561190000000003, player2)
UPDATE t_p15345778_news_shop_project.match_lobbies
SET winner_steam_id = '76561190000000003',
    player1_reported_winner = '76561190000000003',
    player2_reported_winner = '76561190000000003',
    status = 'completed',
    updated_at = NOW()
WHERE id = 2;

INSERT INTO t_p15345778_news_shop_project.lobby_messages (lobby_id, steam_id, persona_name, message)
VALUES (2, 'system', 'Система', '🏆 Победитель определён: Player_Gamma');

-- Матч 1: победитель Player_Alpha (76561190000000001, player1)
UPDATE t_p15345778_news_shop_project.match_lobbies
SET winner_steam_id = '76561190000000001',
    player1_reported_winner = '76561190000000001',
    player2_reported_winner = '76561190000000001',
    status = 'completed',
    updated_at = NOW()
WHERE id = 3;

INSERT INTO t_p15345778_news_shop_project.lobby_messages (lobby_id, steam_id, persona_name, message)
VALUES (3, 'system', 'Система', '🏆 Победитель определён: Player_Alpha');

-- Продвигаем победителей в полуфинал
-- Матч 0 → round 1, match 0, slot player2 (0%2=0 → player1)
INSERT INTO t_p15345778_news_shop_project.match_lobbies
    (tournament_id, round_index, match_index, status, player1_steam_id)
VALUES (9, 1, 0, 'waiting', '76561190000000003')
ON CONFLICT (tournament_id, round_index, match_index)
DO UPDATE SET player1_steam_id = '76561190000000003', updated_at = NOW();

-- Матч 1 → round 1, match 0, slot player2 (1%2=1 → player2)
UPDATE t_p15345778_news_shop_project.match_lobbies
SET player2_steam_id = '76561190000000001', updated_at = NOW()
WHERE tournament_id = 9 AND round_index = 1 AND match_index = 0;