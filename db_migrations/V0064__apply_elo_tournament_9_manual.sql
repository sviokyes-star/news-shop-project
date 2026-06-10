UPDATE t_p15345778_news_shop_project.tournaments SET status = 'completed' WHERE id = 9;

INSERT INTO t_p15345778_news_shop_project.player_rankings (steam_id, persona_name, game, points, wins, losses) VALUES
    ('76561198974174275', 'SvI',            'hearthstone', 1048, 3, 0),
    ('76561190000000001', 'Player_Alpha',   'hearthstone', 1016, 2, 1),
    ('76561190000000003', 'Player_Gamma',   'hearthstone', 1000, 1, 1),
    ('76561190000000006', 'Player_Zeta',    'hearthstone', 1000, 1, 1),
    ('76561190000000007', 'Player_Eta',     'hearthstone', 984,  0, 1),
    ('76561190000000002', 'Player_Beta',    'hearthstone', 984,  0, 1),
    ('76561190000000005', 'Player_Epsilon', 'hearthstone', 984,  0, 1),
    ('76561190000000004', 'Player_Delta',   'hearthstone', 984,  0, 1)
ON CONFLICT (steam_id, game) DO UPDATE SET points = EXCLUDED.points, wins = EXCLUDED.wins, losses = EXCLUDED.losses;