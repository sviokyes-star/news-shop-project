UPDATE t_p15345778_news_shop_project.match_lobbies
SET winner_steam_id = '76561190000000001',
    player1_reported_winner = '76561190000000001',
    player2_reported_winner = '76561190000000001',
    status = 'completed',
    updated_at = NOW()
WHERE id = 5;

INSERT INTO t_p15345778_news_shop_project.lobby_messages (lobby_id, steam_id, persona_name, message)
VALUES (5, 'system', 'Система', '🏆 Победитель определён: Player_Alpha');

-- match_index=0 → round 2, match 0 (0//2=0), player1 (0%2=0)
INSERT INTO t_p15345778_news_shop_project.match_lobbies
    (tournament_id, round_index, match_index, status, player1_steam_id)
VALUES (9, 2, 0, 'waiting', '76561190000000001')
ON CONFLICT (tournament_id, round_index, match_index)
DO UPDATE SET player1_steam_id = '76561190000000001', updated_at = NOW();