import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import Icon from '@/components/ui/icon';
import { formatShortDate } from '@/utils/dateFormat';

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
  };
  tournaments: Tournament[];
  statistics: {
    tournaments_count: number;
    purchases_count: number;
    total_spent: number;
  };
}

const UserProfile = () => {
  const { steamId } = useParams<{ steamId: string }>();
  const navigate = useNavigate();
  const [profileData, setProfileData] = useState<ProfileData | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);

  useEffect(() => {
    if (!steamId) {
      navigate('/');
      return;
    }
    const savedUser = localStorage.getItem('steamUser');
    if (savedUser) {
      const me = JSON.parse(savedUser);
      if (me.steamId === steamId) {
        navigate('/profile', { replace: true });
        return;
      }
    }
    loadProfile(steamId);
  }, [steamId]);

  const loadProfile = async (id: string) => {
    setIsLoading(true);
    try {
      const response = await fetch(
        `https://functions.poehali.dev/88f7bd27-aac7-4eab-b045-2d423b092ebb?steam_id=${id}`
      );
      const data = await response.json();
      if (!data.user || !data.user.steamId) {
        setNotFound(true);
      } else {
        setProfileData(data);
      }
    } catch {
      setNotFound(true);
    } finally {
      setIsLoading(false);
    }
  };

  if (isLoading) {
    return (
      <main className="container mx-auto px-6 py-16 flex justify-center">
        <Icon name="Loader2" size={40} className="animate-spin text-primary" />
      </main>
    );
  }

  if (notFound || !profileData) {
    return (
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
  }

  const { user, tournaments, statistics } = profileData;
  const displayName = user.nickname || user.personaName;

  return (
    <main className="container mx-auto px-6 py-16">
      <div className="space-y-10">
        <Button onClick={() => navigate(-1)} variant="ghost" className="gap-2 -ml-2 mb-2">
          <Icon name="ArrowLeft" size={16} />
          Назад
        </Button>

        <Card className="p-10 border border-border bg-gradient-to-br from-primary/5 to-primary/10 backdrop-blur border-primary/20">
          <div className="flex items-start gap-8">
            <img
              src={user.avatarUrl}
              alt={displayName}
              className="w-32 h-32 rounded-2xl border-4 border-primary shadow-2xl shadow-primary/20"
            />
            <div className="flex-1 space-y-4">
              <div>
                <h1 className="text-4xl font-bold tracking-tight">{displayName}</h1>
                {user.nickname && (
                  <p className="text-muted-foreground mt-1">Steam: {user.personaName}</p>
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

              {user.isBlocked && (
                <div className="flex items-center gap-2 px-4 py-3 rounded-xl bg-destructive/10 border border-destructive/30">
                  <Icon name="Ban" size={18} className="text-destructive" />
                  <div>
                    <p className="text-sm font-semibold text-destructive">Пользователь заблокирован</p>
                    {user.blockReason && (
                      <p className="text-xs text-muted-foreground">{user.blockReason}</p>
                    )}
                  </div>
                </div>
              )}

              <div className="grid grid-cols-2 gap-6 pt-4">
                <div className="p-4 rounded-xl bg-background/50 border border-border">
                  <div className="flex items-center gap-2 mb-2">
                    <Icon name="Trophy" size={20} className="text-primary" />
                    <p className="text-sm text-muted-foreground">Турниров</p>
                  </div>
                  <p className="text-3xl font-bold">{statistics.tournaments_count}</p>
                </div>

                <div className="p-4 rounded-xl bg-background/50 border border-border">
                  <div className="flex items-center gap-2 mb-2">
                    <Icon name="ShoppingBag" size={20} className="text-primary" />
                    <p className="text-sm text-muted-foreground">Покупок</p>
                  </div>
                  <p className="text-3xl font-bold">{statistics.purchases_count}</p>
                </div>
              </div>
            </div>
          </div>
        </Card>

        <div className="space-y-6">
          <div>
            <h2 className="text-3xl font-bold mb-2">Турниры</h2>
            <p className="text-muted-foreground">История участия в турнирах</p>
          </div>

          {tournaments.length > 0 ? (
            <div className="space-y-4">
              {tournaments.map((tournament) => (
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
                        {tournament.tournament_type === 'vip' && (
                          <div className="px-3 py-1 bg-yellow-500/20 border border-yellow-500/30 rounded-full">
                            <span className="text-xs font-bold text-yellow-500">VIP</span>
                          </div>
                        )}
                        <div className="px-3 py-1 bg-primary/20 rounded-full">
                          <span className="text-xs font-bold text-primary">#{tournament.registration_position} место регистрации</span>
                        </div>
                      </div>

                      <div>
                        <h3 className="text-xl font-bold mb-1">{tournament.name}</h3>
                        <p className="text-muted-foreground text-sm">{tournament.description}</p>
                      </div>

                      <div className="flex items-center gap-6 text-sm">
                        <div className="flex items-center gap-2">
                          <Icon name="DollarSign" size={16} className="text-primary" />
                          <span className="text-muted-foreground">Призовой фонд:</span>
                          <span className="font-bold text-primary">{tournament.prize_pool.toLocaleString('ru-RU')}₽</span>
                        </div>
                        <div className="flex items-center gap-2">
                          <Icon name="Calendar" size={16} className="text-muted-foreground" />
                          <span className="text-muted-foreground">{formatShortDate(tournament.start_date)}</span>
                        </div>
                      </div>
                    </div>
                    <Icon name="ChevronRight" size={20} className="text-muted-foreground ml-4 mt-1" />
                  </div>
                </Card>
              ))}
            </div>
          ) : (
            <Card className="p-12 border border-border bg-card/50 text-center">
              <Icon name="Trophy" size={48} className="text-muted-foreground mx-auto mb-4" />
              <p className="text-muted-foreground">Пользователь ещё не участвовал в турнирах</p>
            </Card>
          )}
        </div>
      </div>
    </main>
  );
};

export default UserProfile;