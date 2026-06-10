import { Card } from '@/components/ui/card';
import Icon from '@/components/ui/icon';
import { formatDateTime } from '@/utils/dateFormat';
import { TournamentDetail } from './types';

interface TournamentInfoProps {
  tournament: TournamentDetail;
}

const TournamentInfo = ({ tournament }: TournamentInfoProps) => {
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

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-2 flex-wrap">
        <span className="px-3 py-1 rounded-full text-sm font-medium border bg-primary/10 text-primary border-primary/20">
          {tournament.game || 'CS2'}
        </span>
        <span className={`px-3 py-1 rounded-full text-sm font-medium border ${getStatusColor(tournament.status)}`}>
          {getStatusText(tournament.status)}
        </span>
        <span className={`px-3 py-1 rounded-full text-sm font-medium border ${getTypeColor(tournament.tournament_type)}`}>
          {tournament.tournament_type}
        </span>
        {tournament.is_rated && (
          <span className="flex items-center gap-1.5 px-3 py-1 rounded-full text-sm font-medium border bg-yellow-500/10 text-yellow-500 border-yellow-500/20">
            <Icon name="Star" size={13} />
            Рейтинговый
          </span>
        )}
      </div>

      <div>
        <h1 className="text-4xl font-bold mb-2">{tournament.name}</h1>
        <p className="text-muted-foreground text-lg">{tournament.description}</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <Card className="p-4">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-full bg-primary/10 flex items-center justify-center">
              <Icon name="DollarSign" size={20} className="text-primary" />
            </div>
            <div>
              <p className="text-sm text-muted-foreground">Призовой фонд</p>
              <p className="text-xl font-bold">{tournament.prize_pool.toLocaleString('ru-RU', { timeZone: 'Europe/Moscow' })} ₽</p>
            </div>
          </div>
        </Card>

        <Card className="p-4">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-full bg-primary/10 flex items-center justify-center">
              <Icon name="Users" size={20} className="text-primary" />
            </div>
            <div>
              <p className="text-sm text-muted-foreground">Участники</p>
              <p className="text-xl font-bold">{tournament.participants_count}/{tournament.max_participants}</p>
            </div>
          </div>
        </Card>

        <Card className="p-4">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-full bg-primary/10 flex items-center justify-center">
              <Icon name="Calendar" size={20} className="text-primary" />
            </div>
            <div>
              <p className="text-sm text-muted-foreground">Начало</p>
              <p className="text-xl font-bold">{formatDateTime(tournament.start_date)}</p>
            </div>
          </div>
        </Card>
      </div>
    </div>
  );
};

export default TournamentInfo;