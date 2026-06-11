ALTER TABLE t_p15345778_news_shop_project.shop_items
  ADD COLUMN IF NOT EXISTS unit_multiplier INTEGER NOT NULL DEFAULT 1;

UPDATE t_p15345778_news_shop_project.shop_items
SET unit_multiplier = 1000,
    slider_min = 1,
    slider_max = 1000,
    slider_step = 1,
    unit_price = 1,
    unit_name = '₽'
WHERE id = 8;