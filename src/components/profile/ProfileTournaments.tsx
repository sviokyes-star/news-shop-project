import { useNavigate } from 'react-router-dom';
import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import Icon from '@/components/ui/icon';
import { formatDateTime, formatShortDate } from '@/utils/dateFormat';

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

interface ProfileTournamentsProps {
  tournaments: Tournament[];
}

export default function ProfileTournaments({ tournaments }: ProfileTournamentsProps) {
  const navigate = useNavigate();

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-3xl font-bold mb-2">Мои турниры</h2>
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
                      <span className="font-bold">{formatShortDate(tournament.start_date)}</span>
                    </div>
                  </div>

                  <div className="pt-3 border-t border-border">
                    <div className="flex items-center gap-2 text-sm text-muted-foreground">
                      <Icon name="Clock" size={14} />
                      <span>Зарегистрирован {formatDateTime(tournament.registered_at)}</span>
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
  );
}
