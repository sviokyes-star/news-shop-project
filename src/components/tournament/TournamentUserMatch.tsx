import { useNavigate } from 'react-router-dom';
import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import Icon from '@/components/ui/icon';
import { TournamentDetail, Participant } from './types';

interface UserMatchResult {
  roundIndex: number;
  matchIndex: number;
  players: (Participant | null)[];
  winnerSteamId?: string | null;
}

interface TournamentUserMatchProps {
  tournament: TournamentDetail;
  userMatch: UserMatchResult | null;
}

export default function TournamentUserMatch({ tournament, userMatch }: TournamentUserMatchProps) {
  const navigate = useNavigate();

  const pool = tournament.participants.filter(p => p.confirmed_at).length >= 2
    ? tournament.participants.filter(p => p.confirmed_at)
    : tournament.participants;
  const totalRounds = pool.length >= 2
    ? Math.ceil(Math.log2(Math.pow(2, Math.ceil(Math.log2(Math.max(pool.length, 2))))))
    : 1;
  const getStageName = (rIdx: number) => {
    const fromEnd = totalRounds - 1 - rIdx;
    if (fromEnd === 0) return 'Финал';
    if (fromEnd === 1) return 'Полуфинал';
    if (fromEnd === 2) return '1/4 финала';
    if (fromEnd === 3) return '1/8 финала';
    return `Раунд ${rIdx + 1}`;
  };

  return (
    <Card className="p-6 border-border bg-card/60">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-xl font-bold flex items-center gap-2">
          <Icon name="Swords" size={20} className="text-primary" />
          Ваш матч
        </h3>
        {userMatch && (
          <span className="text-sm font-semibold px-3 py-1 rounded-full bg-primary/15 text-primary border border-primary/30">
            {getStageName(userMatch.roundIndex)}
          </span>
        )}
      </div>

      {userMatch ? (
        <div className="space-y-4">
          <div className="flex items-center p-4 rounded-xl bg-background/50 border border-border">
            {/* Игрок 1 */}
            <div className="flex items-center gap-3 flex-1 min-w-0">
              {userMatch.players[0] ? (
                <>
                  {userMatch.players[0].avatar_url
                    ? <img src={userMatch.players[0].avatar_url} className="w-10 h-10 rounded-lg border border-border flex-shrink-0" />
                    : <div className="w-10 h-10 rounded-lg bg-primary/20 flex-shrink-0" />
                  }
                  <div className="min-w-0">
                    <p className="font-semibold text-sm truncate">{userMatch.players[0].persona_name}</p>
                    <p className="text-xs text-muted-foreground">{userMatch.players[0].rating ?? 0} pts</p>
                  </div>
                </>
              ) : (
                <p className="text-sm text-muted-foreground italic">TBD</p>
              )}
            </div>

            {/* VS по центру */}
            <div className="flex-shrink-0 px-4">
              <span className="text-lg font-bold text-muted-foreground">VS</span>
            </div>

            {/* Игрок 2 */}
            <div className="flex items-center gap-3 flex-1 min-w-0 flex-row-reverse">
              {userMatch.players[1] ? (
                <>
                  {userMatch.players[1].avatar_url
                    ? <img src={userMatch.players[1].avatar_url} className="w-10 h-10 rounded-lg border border-border flex-shrink-0" />
                    : <div className="w-10 h-10 rounded-lg bg-primary/20 flex-shrink-0" />
                  }
                  <div className="min-w-0 text-right">
                    <p className="font-semibold text-sm truncate">{userMatch.players[1].persona_name}</p>
                    <p className="text-xs text-muted-foreground">{userMatch.players[1].rating ?? 0} pts</p>
                  </div>
                </>
              ) : (
                <p className="text-sm text-muted-foreground italic">TBD</p>
              )}
            </div>
          </div>

          {userMatch.winnerSteamId ? (
            <div className={`flex items-center justify-center gap-2 px-4 py-3 rounded-xl border text-sm font-semibold ${
              userMatch.players.some(p => p?.steam_id === userMatch.winnerSteamId && p?.steam_id === tournament.participants.find(x => x.steam_id === p?.steam_id)?.steam_id)
                ? 'bg-green-500/10 border-green-500/30 text-green-500'
                : 'bg-muted/30 border-border text-muted-foreground'
            }`}>
              <Icon name="Trophy" size={15} />
              {(() => {
                const winner = userMatch.players.find(p => p?.steam_id === userMatch.winnerSteamId);
                return `Победитель: ${winner?.persona_name ?? userMatch.winnerSteamId}`;
              })()}
            </div>
          ) : userMatch.players[0] && userMatch.players[1] ? (
            <Button
              className="w-full gap-2"
              onClick={() => {
                const p1 = userMatch.players[0]?.steam_id ?? '';
                const p2 = userMatch.players[1]?.steam_id ?? '';
                navigate(`/tournament/${tournament.id}/match/${userMatch.roundIndex}/${userMatch.matchIndex}?p1=${p1}&p2=${p2}`);
              }}
            >
              <Icon name="DoorOpen" size={18} />
              Войти в лобби матча
            </Button>
          ) : (
            <div className="flex items-center justify-center gap-2 px-4 py-3 rounded-xl bg-muted/30 border border-border text-sm text-muted-foreground">
              <Icon name="Clock" size={15} />
              Ожидаем соперника из другого матча...
            </div>
          )}
        </div>
      ) : (
        <p className="text-muted-foreground text-sm">Сетка ещё формируется или вы не в первом раунде</p>
      )}
    </Card>
  );
}