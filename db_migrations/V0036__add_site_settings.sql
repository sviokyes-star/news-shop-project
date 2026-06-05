CREATE TABLE t_p15345778_news_shop_project.site_settings (
    key VARCHAR(100) PRIMARY KEY,
    value TEXT,
    updated_at TIMESTAMP DEFAULT NOW()
);

INSERT INTO t_p15345778_news_shop_project.site_settings (key, value)
VALUES ('logo_url', 'https://cdn.poehali.dev/projects/0cd5ea72-8c09-43b2-b92c-a0fdee84371e/files/favicon-1771578220171.png')
ON CONFLICT (key) DO NOTHING;
