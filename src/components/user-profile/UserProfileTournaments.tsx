import { useNavigate } from 'react-router-dom';
import { Card } from '@/components/ui/card';
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

interface UserProfileTournamentsProps {
  tournaments: Tournament[];
}

export default function UserProfileTournaments({ tournaments }: UserProfileTournamentsProps) {
  const navigate = useNavigate();

  return (
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
  );
}
