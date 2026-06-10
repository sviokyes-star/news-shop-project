import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import Icon from '@/components/ui/icon';
import { TournamentFormData } from './types';
import TournamentAdminPicker from './TournamentAdminPicker';

interface TournamentFormProps {
  formData: TournamentFormData;
  onFormChange: (data: TournamentFormData) => void;
  onSubmit: () => void;
  onCancel: () => void;
  isEditing?: boolean;
  tournamentId?: number;
  adminSteamId: string;
}

export default function TournamentForm({
  formData,
  onFormChange,
  onSubmit,
  onCancel,
  isEditing = false,
  tournamentId,
  adminSteamId,
}: TournamentFormProps) {
  const setFormData = (updates: Partial<TournamentFormData>) => {
    onFormChange({ ...formData, ...updates });
  };

  const idPrefix = isEditing ? `edit-${tournamentId}` : '';

  return (
    <Card className={`p-6 ${isEditing ? '' : 'bg-card/80 backdrop-blur border-primary/20'}`}>
      <h3 className="text-xl font-bold mb-4 flex items-center gap-2">
        <Icon name={isEditing ? "Edit" : "Plus"} size={20} className="text-primary" />
        {isEditing ? 'Редактирование турнира' : 'Новый турнир'}
      </h3>
      <div className="grid gap-4">
        <div>
          <Label htmlFor={`${idPrefix}-name`}>Название *</Label>
          <Input
            id={`${idPrefix}-name`}
            value={formData.name}
            onChange={(e) => setFormData({ name: e.target.value })}
            placeholder="Введите название турнира"
          />
        </div>

        <div>
          <Label htmlFor={`${idPrefix}-description`}>Описание</Label>
          <Textarea
            id={`${idPrefix}-description`}
            value={formData.description}
            onChange={(e) => setFormData({ description: e.target.value })}
            placeholder="Введите описание турнира"
            rows={3}
          />
        </div>

        <div>
          <Label htmlFor={`${idPrefix}-rules`}>Правила</Label>
          <Textarea
            id={`${idPrefix}-rules`}
            value={formData.rules}
            onChange={(e) => setFormData({ rules: e.target.value })}
            placeholder="Опишите правила турнира..."
            rows={4}
          />
        </div>

        <div>
          <Label htmlFor={`${idPrefix}-prizes_description`}>Призы</Label>
          <Textarea
            id={`${idPrefix}-prizes_description`}
            value={formData.prizes_description}
            onChange={(e) => setFormData({ prizes_description: e.target.value })}
            placeholder="1 место — ..., 2 место — ..."
            rows={3}
          />
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <Label htmlFor={`${idPrefix}-prize_pool`}>Призовой фонд (₽) *</Label>
            <Input
              id={`${idPrefix}-prize_pool`}
              type="number"
              value={formData.prize_pool}
              onChange={(e) => setFormData({ prize_pool: e.target.value })}
              placeholder="10000"
            />
          </div>

          <div>
            <Label htmlFor={`${idPrefix}-max_participants`}>Макс. участников *</Label>
            <Input
              id={`${idPrefix}-max_participants`}
              type="number"
              value={formData.max_participants}
              onChange={(e) => setFormData({ max_participants: e.target.value })}
              placeholder="32"
            />
          </div>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <Label htmlFor={`${idPrefix}-bracket_type`}>Создание сетки</Label>
            <select
              id={`${idPrefix}-bracket_type`}
              value={formData.bracket_type}
              onChange={(e) => setFormData({ bracket_type: e.target.value })}
              className="w-full px-3 py-2 rounded-lg border border-border bg-background text-foreground"
            >
              <option value="random">Случайно</option>
              <option value="rating">По рейтингу</option>
            </select>
          </div>

          <div>
            <Label htmlFor={`${idPrefix}-tournament_type`}>Тип турнира</Label>
            <select
              id={`${idPrefix}-tournament_type`}
              value={formData.tournament_type}
              onChange={(e) => setFormData({ tournament_type: e.target.value })}
              className="w-full px-3 py-2 rounded-lg border border-border bg-background text-foreground"
            >
              <option value="solo">Соло</option>
              <option value="team">Командный</option>
              <option value="weekly">Еженедельный</option>
            </select>
          </div>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <Label htmlFor={`${idPrefix}-game`}>Игра</Label>
            <select
              id={`${idPrefix}-game`}
              value={formData.game}
              onChange={(e) => setFormData({ game: e.target.value })}
              className="w-full px-3 py-2 rounded-lg border border-border bg-background text-foreground"
            >
              <option value="CS2">CS2</option>
              <option value="Dota 2">Dota 2</option>
              <option value="Valorant">Valorant</option>
              <option value="League of Legends">League of Legends</option>
              <option value="Overwatch 2">Overwatch 2</option>
              <option value="Hearthstone">Hearthstone</option>
            </select>
          </div>

          <div>
            <Label htmlFor={`${idPrefix}-status`}>Статус</Label>
            <select
              id={`${idPrefix}-status`}
              value={formData.status}
              onChange={(e) => setFormData({ status: e.target.value })}
              className="w-full px-3 py-2 rounded-lg border border-border bg-background text-foreground"
            >
              <option value="open">Открыт (регистрация)</option>
              <option value="upcoming">Предстоящий</option>
              <option value="active">Активный</option>
              <option value="completed">Завершен</option>
            </select>
          </div>
        </div>

        <div>
          <Label htmlFor={`${idPrefix}-start_date`}>Дата начала *</Label>
          <Input
            id={`${idPrefix}-start_date`}
            type="datetime-local"
            value={formData.start_date}
            onChange={(e) => setFormData({ start_date: e.target.value })}
          />
        </div>

        <div
          className="flex items-center gap-3 px-4 py-3 rounded-lg border border-border bg-muted/20 cursor-pointer select-none"
          onClick={() => setFormData({ is_rated: !formData.is_rated })}
        >
          <div className={`w-5 h-5 rounded border-2 flex items-center justify-center flex-shrink-0 transition-colors ${formData.is_rated ? 'bg-primary border-primary' : 'border-border bg-background'}`}>
            {formData.is_rated && <Icon name="Check" size={12} className="text-primary-foreground" />}
          </div>
          <div>
            <p className="text-sm font-medium">Рейтинговый турнир</p>
            <p className="text-xs text-muted-foreground">По завершению участникам будет начислен рейтинг Эло</p>
          </div>
        </div>

        <TournamentAdminPicker
          value={formData.admin_steam_ids ?? []}
          onChange={(ids) => setFormData({ admin_steam_ids: ids })}
          adminSteamId={adminSteamId}
        />

        <div className={`flex gap-3 ${isEditing ? '' : 'pt-2 relative z-50'}`}>
          {isEditing ? (
            <>
              <Button onClick={onSubmit} className="flex-1 gap-2">
                <Icon name="Check" size={18} />
                Сохранить
              </Button>
              <Button onClick={onCancel} variant="outline" className="flex-1">
                Отмена
              </Button>
            </>
          ) : (
            <>
              <button
                onClick={() => {
                  console.log('🚀 Starting handleCreate...');
                  try {
                    onSubmit();
                  } catch (err) {
                    console.error('💥 Error in handleCreate:', err);
                    alert('Ошибка: ' + err);
                  }
                }}
                className="flex-1 bg-primary text-primary-foreground px-4 py-2 rounded-md cursor-pointer hover:bg-primary/90 flex items-center justify-center gap-2 transition-all relative z-50"
                type="button"
                style={{ pointerEvents: 'auto' }}
              >
                <Icon name="Check" size={18} />
                Создать
              </button>
              <button 
                onClick={onCancel}
                className="flex-1 border border-input bg-background hover:bg-accent px-4 py-2 rounded-md cursor-pointer flex items-center justify-center gap-2 transition-all"
                type="button"
              >
                Отмена
              </button>
            </>
          )}
        </div>
      </div>
    </Card>
  );
}