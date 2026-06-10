UPDATE t_p15345778_news_shop_project.tournament_registrations
SET confirmed_at = NOW()
WHERE tournament_id = 9 AND steam_id LIKE '7656119000000000%';