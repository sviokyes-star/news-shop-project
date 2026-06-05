import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import Icon from '@/components/ui/icon';
import PlayerLink from '@/components/ui/player-link';
import { formatDateTime, formatShortDate } from '@/utils/dateFormat';
import func2url from '../../backend/func2url.json';
import { toast } from '@/hooks/use-toast';

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
  const [isLoading, setIsLoading] = useState(false);
  const [isEditingNickname, setIsEditingNickname] = useState(false);
  const [newNickname, setNewNickname] = useState('');
  const [nicknameError, setNicknameError] = useState('');
  const [isUploadingAvatar, setIsUploadingAvatar] = useState(false);
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
    try {
      const res = await fetch(`${func2url.friends}?steam_id=${steamId}&action=friends`);
      const data = await res.json();
      setFriends(data.friends || []);
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
      setNewNickname(data.user.nickname || data.user.persona_name || '');
      localStorage.setItem(cacheKey, JSON.stringify(data));
    } catch (error) {
      console.error('Failed to load profile data:', error);
    }
  };

  const handleUpdateNickname = async () => {
    if (!user?.steamId) return;
    
    if (newNickname.trim().length < 3 || newNickname.trim().length > 30) {
      setNicknameError('Никнейм должен быть от 3 до 30 символов');
      return;
    }

    try {
      const response = await fetch(func2url['update-nickname'], {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          steam_id: user.steamId,
          nickname: newNickname.trim()
        })
      });

      const data = await response.json();

      if (data.success) {
        setIsEditingNickname(false);
        setNicknameError('');
        await loadProfileData(user.steamId);
      } else {
        setNicknameError(data.error || 'Ошибка при обновлении никнейма');
      }
    } catch (error) {
      setNicknameError('Ошибка при обновлении никнейма');
    }
  };

  const handleAvatarUpload = async (event: React.ChangeEvent<HTMLInputElement>) => {
    if (!user?.steamId) return;
    
    const file = event.target.files?.[0];
    if (!file) return;

    if (file.size > 5 * 1024 * 1024) {
      toast({
        title: "Ошибка",
        description: "Размер файла не должен превышать 5MB",
        variant: "destructive"
      });
      return;
    }

    if (!file.type.startsWith('image/')) {
      toast({
        title: "Ошибка",
        description: "Можно загружать только изображения",
        variant: "destructive"
      });
      return;
    }

    setIsUploadingAvatar(true);

    try {
      const reader = new FileReader();
      reader.onloadend = async () => {
        const base64String = reader.result as string;

        const response = await fetch(func2url['upload-avatar'], {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            steam_id: user.steamId,
            image: base64String
          })
        });

        const data = await response.json();

        if (data.success) {
          setUser({ ...user, avatarUrl: data.avatar_url });
          localStorage.setItem('steamUser', JSON.stringify({ ...user, avatarUrl: data.avatar_url }));
          await loadProfileData(user.steamId);
          toast({
            title: "Успешно!",
            description: "Аватар обновлён"
          });
        } else {
          toast({
            title: "Ошибка",
            description: data.error || 'Ошибка при загрузке аватара',
            variant: "destructive"
          });
        }

        setIsUploadingAvatar(false);
      };

      reader.readAsDataURL(file);
    } catch (error) {
      toast({
        title: "Ошибка",
        description: "Ошибка при загрузке аватара",
        variant: "destructive"
      });
      setIsUploadingAvatar(false);
    }
  };



  if (!user) {
    return null;
  }

  return (
      <main className="container mx-auto px-6 py-16">
        <div className="space-y-10">
          {friendRequests.length > 0 && (
            <Card className="p-6 border border-primary/30 bg-primary/5">
              <div className="flex items-center gap-2 mb-4">
                <Icon name="UserPlus" size={20} className="text-primary" />
                <h2 className="text-lg font-bold">Заявки в друзья</h2>
                <span className="ml-auto px-2 py-0.5 bg-primary text-primary-foreground text-xs font-bold rounded-full">{friendRequests.length}</span>
              </div>
              <div className="space-y-3">
                {friendRequests.map(req => (
                  <div key={req.steamId} className="flex items-center gap-3">
                    <PlayerLink steamId={req.steamId} name={req.personaName} avatarOnly avatarUrl={req.avatarUrl} avatarSize={10} />
                    <PlayerLink steamId={req.steamId} name={req.personaName} className="flex-1" />
                    <div className="flex gap-2">
                      <Button size="sm" onClick={() => handleAcceptFriend(req.steamId)} className="gap-1">
                        <Icon name="Check" size={14} />
                        Принять
                      </Button>
                      <Button size="sm" variant="outline" onClick={() => handleDeclineFriend(req.steamId)} className="gap-1">
                        <Icon name="X" size={14} />
                      </Button>
                    </div>
                  </div>
                ))}
              </div>
            </Card>
          )}

          <Card className="p-10 border border-border bg-gradient-to-br from-primary/5 to-primary/10 backdrop-blur border-primary/20">
            <div className="flex items-start gap-8">
              <div className="relative group">
                <img 
                  src={user.avatarUrl} 
                  alt={user.personaName}
                  className="w-32 h-32 rounded-2xl border-4 border-primary shadow-2xl shadow-primary/20"
                />
                <label 
                  htmlFor="avatar-upload"
                  className="absolute inset-0 bg-black/60 rounded-2xl flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity cursor-pointer"
                >
                  {isUploadingAvatar ? (
                    <Icon name="Loader2" size={32} className="text-white animate-spin" />
                  ) : (
                    <div className="text-center">
                      <Icon name="Camera" size={32} className="text-white mx-auto mb-1" />
                      <p className="text-xs text-white font-medium">Сменить</p>
                    </div>
                  )}
                </label>
                <input
                  id="avatar-upload"
                  type="file"
                  accept="image/*"
                  onChange={handleAvatarUpload}
                  className="hidden"
                  disabled={isUploadingAvatar}
                />
              </div>
              <div className="flex-1 space-y-4">
                <div>
                  {isEditingNickname ? (
                    <div className="space-y-3">
                      <div className="flex items-center gap-3">
                        <Input
                          value={newNickname}
                          onChange={(e) => setNewNickname(e.target.value)}
                          placeholder="Введите никнейм"
                          className="text-2xl font-bold h-12"
                          maxLength={30}
                        />
                        <Button onClick={handleUpdateNickname} size="sm" className="gap-2">
                          <Icon name="Check" size={16} />
                          Сохранить
                        </Button>
                        <Button 
                          onClick={() => {
                            setIsEditingNickname(false);
                            setNicknameError('');
                            setNewNickname(profileData?.user.nickname || user.personaName);
                          }} 
                          variant="outline" 
                          size="sm"
                        >
                          <Icon name="X" size={16} />
                        </Button>
                      </div>
                      {nicknameError && (
                        <p className="text-sm text-destructive">{nicknameError}</p>
                      )}
                    </div>
                  ) : (
                    <div className="flex items-center gap-3">
                      <h1 className="text-4xl font-bold tracking-tight">{profileData?.user.nickname || user.personaName}</h1>
                      <Button 
                        onClick={() => setIsEditingNickname(true)} 
                        variant="ghost" 
                        size="sm"
                        className="gap-2"
                      >
                        <Icon name="Pencil" size={16} />
                      </Button>
                    </div>
                  )}
                  <a 
                    href={user.profileUrl} 
                    target="_blank" 
                    rel="noopener noreferrer"
                    className="text-primary hover:underline flex items-center gap-2 mt-2"
                  >
                    <Icon name="ExternalLink" size={16} />
                    Открыть профиль Steam
                  </a>
                </div>

                <div className="grid grid-cols-2 gap-6 pt-4">
                  <div className="p-4 rounded-xl bg-gradient-to-br from-primary/20 to-primary/10 border border-primary/30">
                    <div className="flex items-center gap-2 mb-2">
                      <Icon name="Wallet" size={20} className="text-primary" />
                      <p className="text-sm text-muted-foreground">Баланс</p>
                    </div>
                    <p className="text-3xl font-bold text-primary">{profileData.user.balance}₽</p>
                  </div>

                  <div className="p-4 rounded-xl bg-background/50 border border-border">
                    <div className="flex items-center gap-2 mb-2">
                      <Icon name="Trophy" size={20} className="text-primary" />
                      <p className="text-sm text-muted-foreground">Турниров</p>
                    </div>
                    <p className="text-3xl font-bold">{profileData.statistics.tournaments_count}</p>
                  </div>
                </div>
              </div>
            </div>
          </Card>

          <div className="space-y-6">
            <div>
              <h2 className="text-3xl font-bold mb-2">Мои турниры</h2>
              <p className="text-muted-foreground">История участия в турнирах</p>
            </div>

            {profileData.tournaments.length > 0 ? (
              <div className="space-y-4">
                {profileData.tournaments.map((tournament) => (
                  <Card 
                    key={tournament.id}
                    className="p-6 border border-border bg-card/50 backdrop-blur hover:shadow-xl hover:shadow-primary/5 transition-all duration-300 cursor-pointer"
                    onClick={() => navigate(`/tournament/${tournament.id}`)}
                  >
                    <div className="flex items-start justify-between">
                      <div className="flex-1 space-y-3">
                        <div className="flex items-center gap-3 flex-wrap">
                          {tournament.status === 'active' && (
                            <div className="px-3 py-1 bg-primary rounded-full">
                              <span className="text-xs font-bold text-primary-foreground">АКТИВНЫЙ</span>
                            </div>
                          )}
                          {tournament.status === 'open' && (
                            <div className="px-3 py-1 bg-green-500/20 border border-green-500/30 rounded-full">
                              <span className="text-xs font-bold text-green-500">ОТКРЫТА РЕГИСТРАЦИЯ</span>
                            </div>
                          )}
                          {tournament.status === 'upcoming' && (
                            <div className="px-3 py-1 bg-blue-500/20 border border-blue-500/30 rounded-full">
                              <span className="text-xs font-bold text-blue-500">СКОРО</span>
                            </div>
                          )}
                          {tournament.tournament_type === 'vip' && (
                            <div className="px-3 py-1 bg-yellow-500/20 border border-yellow-500/30 rounded-full">
                              <span className="text-xs font-bold text-yellow-500">VIP</span>
                            </div>
                          )}
                          <div className="px-3 py-1 bg-primary/20 rounded-full">
                            <span className="text-xs font-bold text-primary">Место регистрации: #{tournament.registration_position}</span>
                          </div>
                        </div>

                        <div>
                          <h3 className="text-2xl font-bold mb-1">{tournament.name}</h3>
                          <p className="text-muted-foreground">{tournament.description}</p>
                        </div>

                        <div className="flex items-center gap-6 text-sm">
                          <div className="flex items-center gap-2">
                            <Icon name="DollarSign" size={16} className="text-primary" />
                            <span className="text-muted-foreground">Призовой фонд:</span>
                            <span className="font-bold text-primary">{tournament.prize_pool.toLocaleString('ru-RU')}₽</span>
                          </div>
                          <div className="flex items-center gap-2">
                            <Icon name="Users" size={16} className="text-primary" />
                            <span className="text-muted-foreground">Участников:</span>
                            <span className="font-bold">{tournament.max_participants}</span>
                          </div>
                          <div className="flex items-center gap-2">
                            <Icon name="Calendar" size={16} className="text-primary" />
                            <span className="text-muted-foreground">Начало:</span>
                            <span className="font-bold">
                              {formatShortDate(tournament.start_date)}
                            </span>
                          </div>
                        </div>

                        <div className="pt-3 border-t border-border">
                          <div className="flex items-center gap-2 text-sm text-muted-foreground">
                            <Icon name="Clock" size={14} />
                            <span>
                              Зарегистрирован {formatDateTime(tournament.registered_at)}
                            </span>
                          </div>
                        </div>
                      </div>

                      <div className="w-16 h-16 rounded-xl bg-primary/10 flex items-center justify-center">
                        <Icon 
                          name={tournament.tournament_type === 'team' ? 'Users' : tournament.tournament_type === 'weekly' ? 'Zap' : 'Trophy'} 
                          size={32} 
                          className="text-primary" 
                        />
                      </div>
                    </div>
                  </Card>
                ))}
              </div>
            ) : (
              <Card className="p-12 text-center border border-dashed border-border bg-card/30">
                <Icon name="Trophy" size={48} className="text-muted-foreground mx-auto mb-4" />
                <p className="text-xl text-muted-foreground mb-2">Вы ещё не участвовали в турнирах</p>
                <p className="text-muted-foreground mb-6">Зарегистрируйтесь на турнир и начните соревноваться!</p>
                <Button onClick={() => navigate('/?tab=tournaments')}>
                  Перейти к турнирам
                </Button>
              </Card>
            )}
          </div>

          <div className="space-y-6">
            <div>
              <h2 className="text-3xl font-bold mb-2">Друзья</h2>
              <p className="text-muted-foreground">Ваши друзья на сайте</p>
            </div>

            {friends.length > 0 ? (
              <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-4">
                {friends.map(f => (
                  <div key={f.steamId} className="relative">
                    <PlayerLink
                      steamId={f.steamId}
                      name={f.personaName}
                      showAvatar
                      avatarUrl={f.avatarUrl}
                      avatarSize={12}
                      isOnline={f.isOnline}
                      className="flex-col items-center gap-2 p-4 w-full rounded-xl bg-card border border-border hover:border-primary/50 transition-all text-center"
                    />
                  </div>
                ))}
              </div>
            ) : (
              <Card className="p-12 text-center border border-dashed border-border bg-card/30">
                <Icon name="Users" size={48} className="text-muted-foreground mx-auto mb-4" />
                <p className="text-xl text-muted-foreground mb-2">У вас пока нет друзей</p>
                <p className="text-muted-foreground">Найдите игроков и отправьте им заявку в друзья</p>
              </Card>
            )}
          </div>
        </div>
      </main>
  );
};

export default Profile;