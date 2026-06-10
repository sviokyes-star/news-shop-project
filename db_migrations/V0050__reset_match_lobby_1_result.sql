UPDATE t_p15345778_news_shop_project.match_lobbies
SET player1_reported_winner = NULL,
    player2_reported_winner = NULL,
    winner_steam_id = NULL,
    status = 'waiting',
    is_dispute = FALSE
WHERE id = 1;