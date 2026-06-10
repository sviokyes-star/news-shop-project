INSERT INTO t_p15345778_news_shop_project.match_lobbies 
    (tournament_id, round_index, match_index, status, player1_steam_id)
VALUES (9, 1, 1, 'waiting', '76561198974174275')
ON CONFLICT (tournament_id, round_index, match_index) 
DO UPDATE SET player1_steam_id = '76561198974174275', updated_at = NOW();