-- Заявка в друзья от TestBot к SvI
INSERT INTO t_p15345778_news_shop_project.friendships (requester_steam_id, addressee_steam_id, status)
VALUES ('test_user_001', '76561198974174275', 'pending')
ON CONFLICT DO NOTHING;

-- Уведомление для SvI
INSERT INTO t_p15345778_news_shop_project.notifications (steam_id, type, title, body, link)
VALUES ('76561198974174275', 'friend_request', 'Новая заявка в друзья', 'TestBot хочет добавить вас в друзья', '/profile/test_user_001');
