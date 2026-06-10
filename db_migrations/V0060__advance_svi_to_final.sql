-- Устанавливаем победителя и завершаем матч
UPDATE t_p15345778_news_shop_project.match_lobbies
SET winner_steam_id = '76561198974174275',
    status = 'completed',
    updated_at = NOW()
WHERE id = 4;

INSERT INTO t_p15345778_news_shop_project.lobby_messages (lobby_id, steam_id, persona_name, message)
VALUES (4, 'system', 'Система', '🏆 Победитель определён: SvI');

-- Продвигаем SvI в финал: round 1, match 1 → round 2, match 0 (1//2=0), slot player2 (1%2=1)
INSERT INTO t_p15345778_news_shop_project.match_lobbies
    (tournament_id, round_index, match_index, status, player2_steam_id)
VALUES (9, 2, 0, 'waiting', '76561198974174275')
ON CONFLICT (tournament_id, round_index, match_index)
DO UPDATE SET player2_steam_id = '76561198974174275', updated_at = NOW();