-- Создаём матч 3 с Player_Delta vs Player_Zeta, победитель Player_Zeta
INSERT INTO t_p15345778_news_shop_project.match_lobbies
    (tournament_id, round_index, match_index, status, player1_steam_id, player2_steam_id, winner_steam_id, player1_reported_winner, player2_reported_winner)
VALUES (9, 0, 3, 'completed', '76561190000000004', '76561190000000006', '76561190000000006', '76561190000000006', '76561190000000006');

INSERT INTO t_p15345778_news_shop_project.lobby_messages (lobby_id, steam_id, persona_name, message)
SELECT id, 'system', 'Система', '🏆 Победитель определён: Player_Zeta'
FROM t_p15345778_news_shop_project.match_lobbies
WHERE tournament_id = 9 AND round_index = 0 AND match_index = 3;

-- Продвигаем Player_Zeta в полуфинал: match_index=3 → round 1, match 1 (3//2=1), slot player2 (3%2=1)
UPDATE t_p15345778_news_shop_project.match_lobbies
SET player2_steam_id = '76561190000000006', updated_at = NOW()
WHERE tournament_id = 9 AND round_index = 1 AND match_index = 1;