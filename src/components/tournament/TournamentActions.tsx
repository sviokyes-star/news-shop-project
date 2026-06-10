import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import Icon from '@/components/ui/icon';
import { TournamentDetail, SteamUser } from './types';
import { isConfirmationActive, isRegistrationClosed, getConfirmationTimeLeft, getTimeUntilConfirmation } from './utils';

interface TournamentActionsProps {
  tournament: TournamentDetail;
  user: SteamUser | null;
  isRegistered: boolean;
  isFull: boolean;
  isRegistering: boolean;
  isUnregistering: boolean;
  isConfirming: boolean;
  onRegister: () => void;
  onUnregister: () => void;
  onConfirm: () => void;
}

const TournamentActions = ({
  tournament,
  user,
  isRegistered,
  isFull,
  isRegistering,
  isUnregistering,
  isConfirming,
  onRegister,
  onUnregister,
  onConfirm
}: TournamentActionsProps) => {
  const userParticipant = tournament.participants.find(p => p.steam_id === user?.steamId);

  return (
    <Card className="p-6">
      <div className="space-y-4">
        <h3 className="text-xl font-bold">Регистрация</h3>

        {!isRegistered && (
          <Button 
            size="lg" 
            className="w-full py-6 text-lg font-bold"
            onClick={onRegister}
            disabled={isRegistering || isFull || isRegistrationClosed(tournament.start_date)}
          >
            {isRegistering ? (
              <>
                <Icon name="Loader2" size={20} className="mr-2 animate-spin" />
                Регистрация...
              </>
            ) : isFull ? (
              <>
                <Icon name="Users" size={20} className="mr-2" />
                Турнир заполнен
              </>
            ) : isRegistrationClosed(tournament.start_date) ? (
              <>
                <Icon name="Lock" size={20} className="mr-2" />
                Регистрация закрыта
              </>
            ) : (
              <>
                <Icon name="UserPlus" size={20} className="mr-2" />
                Зарегистрироваться на турнир
              </>
            )}
          </Button>
        )}

        {isRegistered && (
          <div className="space-y-3">
            {isConfirmationActive(tournament.start_date) && !userParticipant?.confirmed_at && (
              <div className="p-6 rounded-xl border-2 border-orange-500 bg-orange-500/10">
                <div className="space-y-4">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-3">
                      <Icon name="AlertCircle" size={24} className="text-orange-500" />
                      <div>
                        <p className="font-bold text-lg">Требуется подтверждение!</p>
                        <p className="text-sm text-muted-foreground">Подтвердите участие до начала турнира</p>
                      </div>
                    </div>
                    {getConfirmationTimeLeft(tournament.start_date) && (
                      <div className="text-right">
                        <div className="text-3xl font-bold text-orange-500">
                          {(() => {
                            const time = getConfirmationTimeLeft(tournament.start_date);
                            if (!time) return '';
                            return `${String(time.minutes).padStart(2, '0')}:${String(time.seconds).padStart(2, '0')}`;
                          })()}
                        </div>
                        <div className="text-xs text-muted-foreground">осталось</div>
                      </div>
                    )}
                  </div>
                  <Button 
                    size="lg" 
                    className="w-full py-6 text-lg font-bold bg-orange-500 hover:bg-orange-600"
                    onClick={onConfirm}
                    disabled={isConfirming}
                  >
                    {isConfirming ? (
                      <>
                        <Icon name="Loader2" size={20} className="mr-2 animate-spin" />
                        Подтверждение...
                      </>
                    ) : (
                      <>
                        <Icon name="CheckCircle2" size={20} className="mr-2" />
                        Подтвердить участие
                      </>
                    )}
                  </Button>
                </div>
              </div>
            )}
            
            {userParticipant?.confirmed_at && (
              <div className="p-4 rounded-xl border border-green-500/30 bg-green-500/10">
                <div className="flex items-center gap-2 text-green-500">
                  <Icon name="CheckCircle2" size={20} />
                  <span className="font-semibold">Участие подтверждено</span>
                </div>
              </div>
            )}

            {!isConfirmationActive(tournament.start_date) && !userParticipant?.confirmed_at && getTimeUntilConfirmation(tournament.start_date) && (
              <div className="p-4 rounded-xl border border-blue-500/30 bg-blue-500/10">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <Icon name="Clock" size={20} className="text-blue-500" />
                    <span className="font-semibold text-blue-500">Подтвердить участие через</span>
                  </div>
                  <div className="text-blue-500 font-mono font-bold">
                    {(() => {
                      const time = getTimeUntilConfirmation(tournament.start_date);
                      if (!time) return '';
                      if (time.days > 0) {
                        return `${time.days}д ${String(time.hours).padStart(2, '0')}:${String(time.minutes).padStart(2, '0')}:${String(time.seconds).padStart(2, '0')}`;
                      }
                      return `${String(time.hours).padStart(2, '0')}:${String(time.minutes).padStart(2, '0')}:${String(time.seconds).padStart(2, '0')}`;
                    })()}
                  </div>
                </div>
              </div>
            )}

            {['active', 'ongoing', 'completed'].includes(tournament.status) ? (
              <div className="flex items-center gap-2 px-4 py-3 rounded-xl bg-muted/40 border border-border text-sm text-muted-foreground">
                <Icon name="Lock" size={16} />
                Отмена регистрации недоступна после начала турнира
              </div>
            ) : (
              <Button 
                size="lg" 
                variant="destructive"
                className="w-full py-6 text-lg font-bold"
                onClick={onUnregister}
                disabled={isUnregistering}
              >
                <Icon name="X" size={20} className="mr-2" />
                {isUnregistering ? 'Отмена...' : 'Отменить регистрацию'}
              </Button>
            )}
          </div>
        )}
      </div>
    </Card>
  );
};

export default TournamentActions;