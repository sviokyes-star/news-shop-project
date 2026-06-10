import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import Icon from '@/components/ui/icon';
import { formatShortDateTime } from '@/utils/dateFormat';

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
}

interface TournamentCardProps {
  tournament: Tournament;
  onEdit: () => void;
  onDelete: () => void;
}

export default function TournamentCard({ tournament, onEdit, onDelete }: TournamentCardProps) {
  return (
    <Card className="p-6 bg-card/50 backdrop-blur border-border hover:border-primary/30 transition-colors">
      <div className="flex items-start justify-between gap-6">
        <div className="flex-1 space-y-3">
          <div className="flex items-start gap-3">
            <div className="w-12 h-12 rounded-xl bg-primary/10 flex items-center justify-center flex-shrink-0">
              <Icon 
                name={tournament.tournament_type === 'team' ? 'Users' : tournament.tournament_type === 'weekly' ? 'Zap' : 'Trophy'} 
                size={24} 
                className="text-primary" 
              />
            </div>
            <div className="flex-1">
              <h3 className="text-xl font-bold mb-1">{tournament.name}</h3>
              <p className="text-muted-foreground text-sm">{tournament.description}</p>
            </div>
          </div>

          <div className="grid grid-cols-5 gap-4 text-sm">
            <div className="flex items-center gap-2">
              <Icon name="Gamepad2" size={16} className="text-primary" />
              <span className="text-muted-foreground">Игра:</span>
              <span className="font-bold">{tournament.game || 'CS2'}</span>
            </div>
            <div className="flex items-center gap-2">
              <Icon name="DollarSign" size={16} className="text-primary" />
              <span className="text-muted-foreground">Призовой:</span>
              <span className="font-bold">{tournament.prize_pool.toLocaleString('ru-RU')}₽</span>
            </div>
            <div className="flex items-center gap-2">
              <Icon name="Users" size={16} className="text-primary" />
              <span className="text-muted-foreground">Участников:</span>
              <span className="font-bold">{tournament.participants_count}/{tournament.max_participants}</span>
            </div>
            <div className="flex items-center gap-2">
              <Icon name="Calendar" size={16} className="text-primary" />
              <span className="text-muted-foreground">Начало:</span>
              <span className="font-bold">
                {formatShortDateTime(tournament.start_date)}
              </span>
            </div>
            <div className="flex items-center gap-2">
              <Icon name="Info" size={16} className="text-primary" />
              <span className="text-muted-foreground">Статус:</span>
              <span className="font-bold">
                {tournament.status === 'upcoming' ? 'Предстоящий' : 
                 tournament.status === 'active' ? 'Активный' : 'Завершен'}
              </span>
            </div>
          </div>
        </div>

        <div className="flex gap-2 flex-shrink-0">
          <Button
            type="button"
            onClick={onEdit}
            variant="outline"
            size="sm"
            className="gap-2"
          >
            <Icon name="Edit" size={16} />
            Изменить
          </Button>
          <Button
            type="button"
            onClick={onDelete}
            variant="outline"
            size="sm"
            className="gap-2 text-red-500 hover:text-red-600 hover:border-red-500"
          >
            <Icon name="Trash2" size={16} />
            Удалить
          </Button>
        </div>
      </div>
    </Card>
  );
}