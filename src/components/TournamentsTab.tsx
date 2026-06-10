import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import Icon from '@/components/ui/icon';
import func2url from '../../backend/func2url.json';

interface Tournament {
  id: number;
  name: string;
  description: string;
  prize_pool: number;
  max_participants: number;
  status: string;
  tournament_type: string;
  game: string;
  start_date: string;
  participants_count: number;
  is_registered?: boolean;
  confirmed_at?: string | null;
  is_rated?: boolean;
}

interface SteamUser {
  steamId: string;
  personaName: string;
  avatarUrl: string;
  profileUrl: string;
}

interface TournamentsTabProps {
  tournaments: Tournament[];
  user: SteamUser | null;
  isRegistering: number | null;
  onRegister: (tournamentId: number) => void;
  onUnregister?: (tournamentId: number) => void;
  onConfirm?: (tournamentId: number) => void;
}

interface TopPlayer {
  position: number;
  steam_id: string;
  persona_name: string;
  points: number;
  wins: number;
  losses: number;
}

const TournamentsTab = ({ tournaments, user, isRegistering, onRegister, onUnregister, onConfirm }: TournamentsTabProps) => {
  const navigate = useNavigate();
  const [selectedGame, setSelectedGame] = useState<string>('Все');
  const [leaderboardGame, setLeaderboardGame] = useState<string>('Hearthstone');
  const [topPlayers, setTopPlayers] = useState<TopPlayer[]>([]);
  const [, setTick] = useState(0);

  useEffect(() => {
    fetch(`${func2url.tournaments}?leaderboard_game=${encodeURIComponent(leaderboardGame)}`)
      .then(r => r.json())
      .then(data => setTopPlayers(data.leaderboard || []))
      .catch(() => setTopPlayers([]));
  }, [leaderboardGame]);

  useEffect(() => {
    const timer = setInterval(() => {
      setTick(t => t + 1);
    }, 1000);
    return () => clearInterval(timer);
  }, []);

  const formatDateTime = (dateString: string) => {
    const date = new Date(dateString);
    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `${day}.${month}.${year} ${hours}:${minutes}`;
  };

  const getTimeUntilStart = (dateString: string) => {
    const start = new Date(dateString).getTime();
    const now = Date.now();
    const diff = start - now;

    if (diff <= 0) return null;

    const days = Math.floor(diff / (1000 * 60 * 60 * 24));
    const hours = Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
    const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
    const seconds = Math.floor((diff % (1000 * 60)) / 1000);

    if (days > 0) return `${days}д ${hours}ч`;
    if (hours > 0) return `${hours}ч ${minutes}м`;
    if (minutes > 0) return `${minutes}м ${seconds}с`;
    return `${seconds}с`;
  };

  const isConfirmationActive = (dateString: string) => {
    const start = new Date(dateString).getTime();
    const now = Date.now();
    const oneHourBefore = start - (60 * 60 * 1000);
    
    return now >= oneHourBefore && now < start;
  };

  const isRegistrationClosed = (dateString: string) => {
    const start = new Date(dateString).getTime();
    const now = Date.now();
    const oneHourBefore = start - (60 * 60 * 1000);
    
    return now >= oneHourBefore;
  };

  const getTimeUntilConfirmation = (dateString: string) => {
    const start = new Date(dateString).getTime();
    const now = Date.now();
    const oneHourBefore = start - (60 * 60 * 1000);
    const diff = oneHourBefore - now;

    if (diff <= 0 || now >= start) return null;

    const days = Math.floor(diff / (1000 * 60 * 60 * 24));
    const hours = Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
    const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
    const seconds = Math.floor((diff % (1000 * 60)) / 1000);

    return { days, hours, minutes, seconds };
  };
  

  
  const getPositionBadge = (position: number) => {
    if (position === 1) return '🥇';
    if (position === 2) return '🥈';
    if (position === 3) return '🥉';
    return position;
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'upcoming':
        return 'bg-blue-500/10 text-blue-500 border-blue-500/20';
      case 'ongoing':
        return 'bg-green-500/10 text-green-500 border-green-500/20';
      case 'completed':
        return 'bg-gray-500/10 text-gray-500 border-gray-500/20';
      default:
        return 'bg-muted/10 text-muted-foreground border-muted/20';
    }
  };

  const getStatusText = (status: string) => {
    switch (status) {
      case 'upcoming':
        return 'Скоро';
      case 'ongoing':
        return 'Идёт';
      case 'completed':
        return 'Завершён';
      default:
        return status;
    }
  };

  const getTypeColor = (type: string) => {
    switch (type) {
      case '1v1':
        return 'bg-purple-500/10 text-purple-500 border-purple-500/20';
      case '5v5':
        return 'bg-orange-500/10 text-orange-500 border-orange-500/20';
      case 'battle_royale':
        return 'bg-red-500/10 text-red-500 border-red-500/20';
      default:
        return 'bg-primary/10 text-primary border-primary/20';
    }
  };

  const allGames = ['Все', ...Array.from(new Set(tournaments.map(t => t.game || 'CS2')))];
  const filteredTournaments = selectedGame === 'Все' 
    ? tournaments 
    : tournaments.filter(t => (t.game || 'CS2') === selectedGame);

  return (
    <div className="space-y-10">
      <div className="space-y-3">
        <div className="inline-block px-4 py-1.5 bg-primary/10 border border-primary/20 rounded-full mb-2">
          <span className="text-sm font-medium text-primary">Соревнования</span>
        </div>
        <p className="text-muted-foreground text-xl">Участвуйте в турнирах и выигрывайте призы</p>
      </div>

      <div className="flex gap-3 mb-6 flex-wrap">
        {allGames.map((game) => (
          <Button
            key={game}
            variant={selectedGame === game ? "default" : "outline"}
            onClick={() => setSelectedGame(game)}
            className="gap-2"
          >
            <Icon name={game === 'Все' ? 'Grid3x3' : 'Gamepad2'} size={16} />
            {game}
          </Button>
        ))}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2 space-y-6">
          {filteredTournaments.length > 0 ? (
            filteredTournaments.map((tournament, index) => (
            <Card 
              key={tournament.id}
              className="group p-6 border border-border hover:border-primary/50 transition-all duration-300 hover:shadow-xl hover:shadow-primary/10 bg-card/50 backdrop-blur cursor-pointer"
              style={{ animationDelay: `${index * 100}ms` }}
              onClick={() => navigate(`/tournament/${tournament.id}`)}
            >
              <div className="space-y-4">
                <div className="flex items-start justify-between gap-3">
                  <div className="space-y-2 flex-1">
                    <div className="flex items-center gap-2 flex-wrap">
                      <h3 className="text-xl font-bold tracking-tight group-hover:text-primary transition-colors">
                        {tournament.name}
                      </h3>
                      <span className="px-2 py-0.5 rounded-full text-xs font-medium border bg-primary/10 text-primary border-primary/20">
                        {tournament.game || 'CS2'}
                      </span>
                      <span className={`px-2 py-0.5 rounded-full text-xs font-medium border ${getStatusColor(tournament.status)}`}>
                        {getStatusText(tournament.status)}
                      </span>
                      <span className={`px-2 py-0.5 rounded-full text-xs font-medium border ${getTypeColor(tournament.tournament_type)}`}>
                        {tournament.tournament_type}
                      </span>
                      {tournament.is_rated && (
                        <span className="flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium border bg-yellow-500/10 text-yellow-500 border-yellow-500/20">
                          <Icon name="Star" size={11} />
                          Рейтинговый
                        </span>
                      )}
                    </div>
                    <p className="text-muted-foreground text-sm leading-relaxed">{tournament.description}</p>
                  </div>
                  <div className="w-10 h-10 rounded-full bg-secondary flex items-center justify-center flex-shrink-0 group-hover:bg-primary group-hover:text-primary-foreground transition-all duration-300">
                    <Icon name="Trophy" size={20} />
                  </div>
                </div>

                <div className="flex items-center gap-4 text-xs">
                  <div className="flex items-center gap-1.5 text-muted-foreground">
                    <Icon name="DollarSign" size={14} />
                    <span><strong className="text-foreground">{tournament.prize_pool.toLocaleString('ru-RU', { timeZone: 'Europe/Moscow' })} ₽</strong></span>
                  </div>
                  <div className="flex items-center gap-1.5 text-muted-foreground">
                    <Icon name="Users" size={14} />
                    <span><strong className="text-foreground">{tournament.participants_count}/{tournament.max_participants}</strong></span>
                  </div>
                  <div className="flex items-center gap-1.5 text-muted-foreground">
                    <Icon name="Calendar" size={14} />
                    <span>{formatDateTime(tournament.start_date)}</span>
                  </div>
                  {tournament.status === 'upcoming' && getTimeUntilStart(tournament.start_date) && (
                    <div className="flex items-center gap-1.5 text-primary font-semibold">
                      <Icon name="Clock" size={14} />
                      <span>Начало через {getTimeUntilStart(tournament.start_date)}</span>
                    </div>
                  )}
                </div>

                <div className="flex gap-2 pt-3 border-t border-border" onClick={(e) => e.stopPropagation()}>
                  {tournament.is_registered ? (
                    <>
                      {isConfirmationActive(tournament.start_date) && !tournament.confirmed_at ? (
                        <Button 
                          onClick={(e) => {
                            e.stopPropagation();
                            if (onConfirm) {
                              onConfirm(tournament.id);
                            }
                          }}
                          className="gap-2 text-xs h-9 bg-orange-500 hover:bg-orange-600 flex-1"
                        >
                          <Icon name="CheckCircle2" size={16} />
                          Подтвердить участие
                        </Button>
                      ) : tournament.confirmed_at ? (
                        <Button disabled className="gap-2 text-xs h-9 bg-green-500/20 text-green-500 border-green-500/30 flex-1" variant="secondary">
                          <Icon name="CheckCircle2" size={16} />
                          Участие подтверждено
                        </Button>
                      ) : getTimeUntilConfirmation(tournament.start_date) ? (
                        <Button disabled className="gap-2 text-xs h-9 bg-blue-500/20 text-blue-500 border-blue-500/30 flex-1" variant="secondary">
                          <Icon name="Clock" size={16} />
                          <span>
                            Подтвердить через {(() => {
                              const time = getTimeUntilConfirmation(tournament.start_date);
                              if (!time) return '';
                              if (time.days > 0) {
                                return `${time.days}д ${String(time.hours).padStart(2, '0')}:${String(time.minutes).padStart(2, '0')}:${String(time.seconds).padStart(2, '0')}`;
                              }
                              return `${String(time.hours).padStart(2, '0')}:${String(time.minutes).padStart(2, '0')}:${String(time.seconds).padStart(2, '0')}`;
                            })()}
                          </span>
                        </Button>
                      ) : (
                        <Button disabled className="gap-2 text-xs h-9" variant="secondary">
                          <Icon name="CheckCircle2" size={16} />
                          Вы зарегистрированы
                        </Button>
                      )}
                      {onUnregister && !isRegistrationClosed(tournament.start_date) && (
                        <Button 
                          onClick={(e) => {
                            e.stopPropagation();
                            onUnregister(tournament.id);
                          }}
                          variant="destructive"
                          className="gap-2 text-xs h-9"
                        >
                          <Icon name="UserMinus" size={16} />
                          Отменить
                        </Button>
                      )}
                    </>
                  ) : (
                    <Button 
                      onClick={(e) => {
                        e.stopPropagation();
                        onRegister(tournament.id);
                      }}
                      disabled={isRegistering === tournament.id || tournament.status !== 'upcoming' || isRegistrationClosed(tournament.start_date)}
                      className="gap-2 text-xs h-9"
                    >
                      <Icon name="UserPlus" size={16} />
                      {isRegistering === tournament.id ? 'Регистрация...' : isRegistrationClosed(tournament.start_date) ? 'Регистрация закрыта' : 'Зарегистрироваться'}
                    </Button>
                  )}
                  <Button 
                    variant="outline" 
                    onClick={(e) => {
                      e.stopPropagation();
                      navigate(`/tournament/${tournament.id}`);
                    }}
                    className="gap-2 text-xs h-9"
                  >
                    <Icon name="Info" size={16} />
                    Подробнее
                  </Button>
                </div>
              </div>
            </Card>
          ))
          ) : (
            <Card className="p-12 text-center bg-card/50 backdrop-blur">
              <Icon name="Search" size={48} className="text-muted-foreground mx-auto mb-4" />
              <h3 className="text-xl font-bold mb-2">Турниров не найдено</h3>
              <p className="text-muted-foreground">Попробуйте выбрать другую игру</p>
            </Card>
          )}
        </div>

        <div className="space-y-4">
          <Card className="p-6 bg-card/50 backdrop-blur sticky top-6">
            <div className="space-y-4">
              <div className="space-y-3">
                <h3 className="text-lg font-bold flex items-center gap-2">
                  <Icon name="Trophy" size={20} className="text-primary" />
                  Топ игроков
                </h3>

                <div className="flex gap-2">
                  <button
                    onClick={() => setLeaderboardGame('Hearthstone')}
                    className={`flex-1 p-3 rounded-lg border transition-all ${
                      leaderboardGame === 'Hearthstone' 
                        ? 'bg-primary/10 border-primary shadow-lg shadow-primary/20' 
                        : 'bg-card border-border hover:border-primary/50 hover:bg-primary/5'
                    }`}
                    title="Hearthstone"
                  >
                    <img 
                      src="https://cdn.poehali.dev/files/2aa092b3-feee-432a-b02f-1f6541d0baff.png" 
                      alt="Hearthstone" 
                      className="w-10 h-10 mx-auto object-contain drop-shadow-lg"
                    />
                  </button>
                  <button
                    onClick={() => setLeaderboardGame('Dota 2')}
                    className={`flex-1 p-3 rounded-lg border transition-all ${
                      leaderboardGame === 'Dota 2' 
                        ? 'bg-primary/10 border-primary shadow-lg shadow-primary/20' 
                        : 'bg-card border-border hover:border-primary/50 hover:bg-primary/5'
                    }`}
                    title="Dota 2"
                  >
                    <img 
                      src="https://cdn.poehali.dev/files/24ad0c92-cb66-4860-93d8-9e27370ffc90.png" 
                      alt="Dota 2" 
                      className="w-10 h-10 mx-auto object-contain drop-shadow-lg"
                    />
                  </button>
                  <button
                    onClick={() => setLeaderboardGame('CS2')}
                    className={`flex-1 p-3 rounded-lg border transition-all ${
                      leaderboardGame === 'CS2' 
                        ? 'bg-primary/10 border-primary shadow-lg shadow-primary/20' 
                        : 'bg-card border-border hover:border-primary/50 hover:bg-primary/5'
                    }`}
                    title="Counter-Strike 2"
                  >
                    <img 
                      src="https://cdn.poehali.dev/files/0ec16141-74ad-4adf-8da0-81c446eced42.png" 
                      alt="CS2" 
                      className="w-10 h-10 mx-auto object-contain drop-shadow-lg"
                    />
                  </button>
                </div>
              </div>

              <div className="space-y-1">
                {topPlayers.length === 0 ? (
                  <p className="text-center text-sm text-muted-foreground py-4">Нет данных</p>
                ) : topPlayers.map((player) => (
                  <div
                    key={player.steam_id}
                    className="flex items-center gap-2 p-2 rounded-lg hover:bg-secondary/50 transition-colors text-sm"
                  >
                    <div className="w-6 text-center font-bold text-muted-foreground">
                      {getPositionBadge(Number(player.position))}
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="font-medium truncate">{player.persona_name}</p>
                    </div>
                    <div className="flex items-center gap-2 text-xs text-muted-foreground">
                      <span className="font-bold text-foreground">{player.points}</span>
                      <span>|</span>
                      <span className="text-green-500">{player.wins}W</span>
                      <span>/</span>
                      <span className="text-red-500">{player.losses}L</span>
                    </div>
                  </div>
                ))}
              </div>


            </div>
          </Card>
        </div>
      </div>
    </div>
  );
};

export default TournamentsTab;