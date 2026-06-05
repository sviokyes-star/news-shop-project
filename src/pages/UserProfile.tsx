import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import Icon from '@/components/ui/icon';
import func2url from '../../backend/func2url.json';
import UserProfileHero from '@/components/user-profile/UserProfileHero';
import UserProfileFriends from '@/components/user-profile/UserProfileFriends';
import UserProfileTournaments from '@/components/user-profile/UserProfileTournaments';

interface Tournament {
  id: number;
  name: string;
  description: string;
  prize_pool: number;
  max_participants: number;
  status: string;
  tournament_type: string;
  start_date: string;
  registered_at: string;
  registration_position: number;
}

interface ProfileData {
  user: {
    steamId: string;
    personaName: string;
    nickname?: string;
    avatarUrl: string;
    profileUrl: string;
    balance: number;
    isBlocked: boolean;
    blockReason?: string;
    isOnline?: boolean;
    lastOnline?: string;
  };
  tournaments: Tournament[];
  statistics: {
    tournaments_count: number;
    purchases_count: number;
    total_spent: number;
  };
}

interface Friend {
  steamId: string;
  personaName: string;
  avatarUrl: string;
  isOnline: boolean;
  lastOnline: string;
}

type FriendStatus = 'none' | 'pending' | 'accepted' | 'incoming';

const UserProfile = () => {
  const { steamId } = useParams<{ steamId: string }>();
  const navigate = useNavigate();
  const [profileData, setProfileData] = useState<ProfileData | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);
  const [friends, setFriends] = useState<Friend[]>([]);
  const [friendStatus, setFriendStatus] = useState<FriendStatus>('none');
  const [friendRequester, setFriendRequester] = useState<string | null>(null);
  const [isFriendLoading, setIsFriendLoading] = useState(false);
  const [me, setMe] = useState<{ steamId: string } | null>(null);

  useEffect(() => {
    if (!steamId) { navigate('/'); return; }
    const savedUser = localStorage.getItem('steamUser');
    if (savedUser) {
      const parsed = JSON.parse(savedUser);
      if (parsed.steamId === steamId) { navigate('/profile', { replace: true }); return; }
      setMe(parsed);
    }
    loadProfile(steamId);
  }, [steamId, navigate]);

  useEffect(() => {
    if (steamId && me) loadFriendData(steamId, me.steamId);
  }, [steamId, me]);

  const loadProfile = async (id: string) => {
    setIsLoading(true);
    try {
      const res = await fetch(`https://functions.poehali.dev/88f7bd27-aac7-4eab-b045-2d423b092ebb?steam_id=${id}`);
      const data = await res.json();
      if (!data.user || !data.user.steamId) { setNotFound(true); }
      else { setProfileData(data); }
    } catch { setNotFound(true); }
    finally { setIsLoading(false); }
  };

  const loadFriendData = async (targetId: string, myId: string) => {
    try {
      const [statusRes, friendsRes] = await Promise.all([
        fetch(`${func2url.friends}?steam_id=${myId}&target_id=${targetId}&action=status`),
        fetch(`${func2url.friends}?steam_id=${targetId}&action=friends`),
      ]);
      const statusData = await statusRes.json();
      const friendsData = await friendsRes.json();

      if (statusData.status === 'pending' && statusData.requester === myId) {
        setFriendStatus('pending');
      } else if (statusData.status === 'pending' && statusData.requester === targetId) {
        setFriendStatus('incoming');
        setFriendRequester(targetId);
      } else {
        setFriendStatus(statusData.status || 'none');
      }
      setFriends(friendsData.friends || []);
    } catch (e) {
      console.error('Failed to load friend data', e);
    }
  };

  const handleAddFriend = async () => {
    if (!me || !steamId) return;
    setIsFriendLoading(true);
    await fetch(func2url.friends, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ requester_steam_id: me.steamId, addressee_steam_id: steamId }),
    });
    setFriendStatus('pending');
    setIsFriendLoading(false);
  };

  const handleCancelRequest = async () => {
    if (!me || !steamId) return;
    setIsFriendLoading(true);
    await fetch(func2url.friends, {
      method: 'DELETE',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ steam_id: me.steamId, friend_id: steamId }),
    });
    setFriendStatus('none');
    setIsFriendLoading(false);
  };

  const handleAccept = async () => {
    if (!me || !friendRequester) return;
    setIsFriendLoading(true);
    await fetch(func2url.friends, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ requester_steam_id: friendRequester, addressee_steam_id: me.steamId, action: 'accept' }),
    });
    setFriendStatus('accepted');
    setIsFriendLoading(false);
    if (steamId && me) loadFriendData(steamId, me.steamId);
  };

  const handleDecline = async () => {
    if (!me || !friendRequester) return;
    setIsFriendLoading(true);
    await fetch(func2url.friends, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ requester_steam_id: friendRequester, addressee_steam_id: me.steamId, action: 'decline' }),
    });
    setFriendStatus('none');
    setIsFriendLoading(false);
  };

  const handleRemoveFriend = async () => {
    if (!me || !steamId) return;
    setIsFriendLoading(true);
    await fetch(func2url.friends, {
      method: 'DELETE',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ steam_id: me.steamId, friend_id: steamId }),
    });
    setFriendStatus('none');
    setFriends([]);
    setIsFriendLoading(false);
  };

  if (isLoading) return (
    <main className="container mx-auto px-6 py-16 flex justify-center">
      <Icon name="Loader2" size={40} className="animate-spin text-primary" />
    </main>
  );

  if (notFound || !profileData) return (
    <main className="container mx-auto px-6 py-16 text-center">
      <Icon name="UserX" size={64} className="text-muted-foreground mx-auto mb-4" />
      <h1 className="text-3xl font-bold mb-2">Профиль не найден</h1>
      <p className="text-muted-foreground mb-6">Пользователь с таким Steam ID не существует</p>
      <Button onClick={() => navigate(-1)} variant="outline" className="gap-2">
        <Icon name="ArrowLeft" size={16} />
        Назад
      </Button>
    </main>
  );

  const { user, tournaments, statistics } = profileData;

  return (
    <main className="container mx-auto px-6 py-16">
      <div className="space-y-10">
        <Button onClick={() => navigate(-1)} variant="ghost" className="gap-2 -ml-2">
          <Icon name="ArrowLeft" size={16} />
          Назад
        </Button>

        <UserProfileHero
          user={user}
          statistics={statistics}
          me={me}
          friendStatus={friendStatus}
          isFriendLoading={isFriendLoading}
          onAddFriend={handleAddFriend}
          onCancelRequest={handleCancelRequest}
          onAccept={handleAccept}
          onDecline={handleDecline}
          onRemoveFriend={handleRemoveFriend}
        />

        <UserProfileFriends friends={friends} />

        <UserProfileTournaments tournaments={tournaments} />
      </div>
    </main>
  );
};

export default UserProfile;
