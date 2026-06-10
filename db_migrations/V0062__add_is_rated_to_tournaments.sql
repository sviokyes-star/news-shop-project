ALTER TABLE t_p15345778_news_shop_project.tournaments
    ADD COLUMN IF NOT EXISTS is_rated BOOLEAN NOT NULL DEFAULT false;