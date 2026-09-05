import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import func2url from '@/lib/api';

const BattlenetCallback = () => {
  const navigate = useNavigate();

  useEffect(() => {
    const handleCallback = async () => {
      const params = new URLSearchParams(window.location.search);
      const code = params.get('code');
      const linkSteamId = localStorage.getItem('linkBattlenetToSteam');

      if (!code) {
        console.error('No authorization code found');
        navigate('/');
        return;
      }

      try {
        const redirectUri = `${window.location.origin}/battlenet-callback`;
        const response = await fetch(func2url['battlenet-auth'], {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({
            action: 'callback',
            code,
            redirect_uri: redirectUri,
            link_steam_id: linkSteamId || undefined
          }),
        });

        const data = await response.json();

        if (response.ok) {
          if (data.success && linkSteamId) {
            localStorage.removeItem('linkBattlenetToSteam');
            const savedUser = localStorage.getItem('steamUser');
            if (savedUser) {
              const user = JSON.parse(savedUser);
              user.battlenetId = data.battlenetId;
              user.battletag = data.battletag;
              localStorage.setItem('steamUser', JSON.stringify(user));
            }
            navigate('/profile?linked=battlenet');
          } else {
            const userData = {
              battlenetId: data.battlenetId,
              battletag: data.battletag,
              steamId: data.steamId,
              personaName: data.personaName || data.battletag,
              avatarUrl: data.avatarUrl,
              profileUrl: data.profileUrl,
            };
            localStorage.setItem('steamUser', JSON.stringify(userData));
            navigate('/');
          }
        } else {
          console.error('Battle.net authentication failed:', data.error);
          navigate('/?error=battlenet_auth_failed');
        }
      } catch (error) {
        console.error('Battle.net callback error:', error);
        navigate('/?error=battlenet_callback_failed');
      }
    };

    handleCallback();
  }, [navigate]);

  return (
    <div className="min-h-screen flex items-center justify-center bg-background">
      <div className="text-center space-y-4">
        <div className="animate-spin w-12 h-12 border-4 border-primary border-t-transparent rounded-full mx-auto"></div>
        <p className="text-lg text-muted-foreground">Авторизация через Battle.net...</p>
      </div>
    </div>
  );
};

export default BattlenetCallback;
