import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import func2url from '../../backend/func2url.json';
import { toast } from '@/hooks/use-toast';
import ProfileHero from '@/components/profile/ProfileHero';
import ProfileFriends from '@/components/profile/ProfileFriends';
import ProfileTournaments from '@/components/profile/ProfileTournaments';
import ProfileHistory from '@/components/profile/ProfileHistory';

interface FriendRequest {
  steamId: string;
  personaName: string;
  avatarUrl: string;
  createdAt: string;
}

interface Friend {
  steamId: string;
  personaName: string;
  avatarUrl: string;
  isOnline: boolean;
  lastOnline: string;
}

interface SteamUser {
  steamId: string;
  personaName: string;
  avatarUrl: string;
  profileUrl: string;
  nickname?: string;
}

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
    balance: number;
    isBlocked: boolean;
    blockReason?: string;
    nickname?: string;
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

const Profile = () => {
  const navigate = useNavigate();
  const [user, setUser] = useState<SteamUser | null>(null);
  const [profileData, setProfileData] = useState<ProfileData | null>(null);
  const [friendRequests, setFriendRequests] = useState<FriendRequest[]>([]);
  const [friends, setFriends] = useState<Friend[]>([]);

  useEffect(() => {
    const savedUser = localStorage.getItem('steamUser');
    if (!savedUser) {
      navigate('/');
      return;
    }
    const userData = JSON.parse(savedUser);
    setUser(userData);
    loadProfileData(userData.steamId);
    loadFriendRequests(userData.steamId);
    loadFriends(userData.steamId);
  }, [navigate]);

  const loadFriends = async (steamId: string) => {
    const cacheKey = `friends_${steamId}`;
    const cached = localStorage.getItem(cacheKey);
    if (cached) setFriends(JSON.parse(cached));
    try {
      const res = await fetch(`${func2url.friends}?steam_id=${steamId}&action=friends`);
      const data = await res.json();
      const list = data.friends || [];
      setFriends(list);
      localStorage.setItem(cacheKey, JSON.stringify(list));
    } catch (e) {
      console.error('Failed to load friends', e);
    }
  };

  const loadFriendRequests = async (steamId: string) => {
    try {
      const res = await fetch(`${func2url.friends}?steam_id=${steamId}&action=pending`);
      const data = await res.json();
      setFriendRequests(data.requests || []);
    } catch (e) {
      console.error('Failed to load friend requests', e);
    }
  };

  const handleAcceptFriend = async (requesterSteamId: string) => {
    if (!user) return;
    await fetch(func2url.friends, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ requester_steam_id: requesterSteamId, addressee_steam_id: user.steamId, action: 'accept' }),
    });
    setFriendRequests(prev => prev.filter(r => r.steamId !== requesterSteamId));
    toast({ title: 'Друг добавлен!' });
  };

  const handleDeclineFriend = async (requesterSteamId: string) => {
    if (!user) return;
    await fetch(func2url.friends, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ requester_steam_id: requesterSteamId, addressee_steam_id: user.steamId, action: 'decline' }),
    });
    setFriendRequests(prev => prev.filter(r => r.steamId !== requesterSteamId));
  };

  const loadProfileData = async (steamId: string) => {
    const cacheKey = `profile_${steamId}`;
    const cachedProfile = localStorage.getItem(cacheKey);
    if (cachedProfile) {
      setProfileData(JSON.parse(cachedProfile));
    }
    try {
      const response = await fetch(
        `https://functions.poehali.dev/88f7bd27-aac7-4eab-b045-2d423b092ebb?steam_id=${steamId}`
      );
      const data = await response.json();
      setProfileData(data);
      localStorage.setItem(cacheKey, JSON.stringify(data));
    } catch (error) {
      console.error('Failed to load profile data:', error);
    }
  };

  if (!user || !profileData) return null;

  return (
    <main className="container mx-auto px-6 py-16">
      <div className="space-y-10">
        <ProfileHero
          user={user}
          profileUser={profileData.user}
          statistics={profileData.statistics}
          onUserUpdate={setUser}
          onProfileReload={() => loadProfileData(user.steamId)}
        />

        <ProfileFriends
          friends={friends}
          friendRequests={friendRequests}
          onAccept={handleAcceptFriend}
          onDecline={handleDeclineFriend}
        />

        <ProfileTournaments tournaments={profileData.tournaments} />

        <ProfileHistory steamId={user.steamId} />
      </div>
    </main>
  );
};

export default Profile;