import { useMemo } from 'react';
import { Card } from '@/components/ui/card';
import Icon from '@/components/ui/icon';
import { Participant } from './types';

interface BracketViewProps {
  participants: Participant[];
  maxParticipants: number;
  status: string;
  bracketType: string;
}

function seededShuffle<T>(arr: T[], seed: number): T[] {
  const a = [...arr];
  let s = seed;
  for (let i = a.length - 1; i > 0; i--) {
    s = (s * 1664525 + 1013904223) & 0xffffffff;
    const j = Math.abs(s) % (i + 1);
    [a[i], a[j]] = [a[j], a[i]];
  }
  return a;
}

const STARTED_STATUSES = ['active', 'ongoing', 'completed'];

const BracketView = ({ participants, maxParticipants, status, bracketType }: BracketViewProps) => {
  const hasStarted = STARTED_STATUSES.includes(status);

  const pool = participants.filter(p => p.confirmed_at).length >= 2
    ? participants.filter(p => p.confirmed_at)
    : participants;

  const sorted: Participant[] = useMemo(() => {
    if (!hasStarted || pool.length < 2) return [];
    if (bracketType === 'rating') {
      const byRating = [...pool].sort((a, b) => (b.rating ?? 0) - (a.rating ?? 0));
      const result: Participant[] = [];
      let lo = 0, hi = byRating.length - 1;
      while (lo <= hi) {
        if (lo === hi) { result.push(byRating[lo++]); break; }
        result.push(byRating[lo++]);
        result.push(byRating[hi--]);
      }
      return result;
    }
    const seed = pool.reduce((acc, p) => acc + parseInt(p.steam_id.slice(-4), 16), 0);
    return seededShuffle(pool, seed);
  }, [hasStarted, pool, bracketType]);

  if (!hasStarted) {
    return (
      <Card className="p-12 text-center border border-dashed border-border bg-card/30">
        <Icon name="GitBranch" size={40} className="mx-auto text-muted-foreground mb-4 opacity-30" />
        <p className="text-muted-foreground font-medium">Сетка ещё не сформирована</p>
        <p className="text-sm text-muted-foreground mt-1 opacity-70">Ждите начала турнира</p>
      </Card>
    );
  }

  const size = Math.pow(2, Math.ceil(Math.log2(Math.max(sorted.length, 2))));
  const slots: (Participant | null)[] = [...sorted];
  while (slots.length < size) slots.push(null);

  const firstRound: (Participant | null)[][] = [];
  for (let i = 0; i < slots.length; i += 2) {
    firstRound.push([slots[i], slots[i + 1]]);
  }

  const rounds: (Participant | null)[][][] = [firstRound];
  let roundSize = firstRound.length;
  while (roundSize > 1) {
    roundSize = Math.ceil(roundSize / 2);
    rounds.push(Array.from({ length: roundSize }, () => [null, null]));
  }

  const roundName = (total: number, idx: number) => {
    const fromEnd = total - 1 - idx;
    if (fromEnd === 0) return 'Финал';
    if (fromEnd === 1) return 'Полуфинал';
    if (fromEnd === 2) return '1/4 финала';
    return `Раунд ${idx + 1}`;
  };

  return (
    <div className="overflow-x-auto pb-2">
      <div className="flex gap-4 min-w-max items-start">
        {rounds.map((round, rIdx) => {
          const matchH = 76; // высота одного матча (2 строки ~36px + gap 4px)
          const gap0 = 8;    // отступ между матчами в первом раунде
          const matchWithGap = matchH + gap0;
          const gap = rIdx === 0 ? gap0 : matchWithGap * Math.pow(2, rIdx) - matchH;
          const paddingTop = rIdx === 0 ? 0 : (matchWithGap * Math.pow(2, rIdx - 1) - matchWithGap) / 2;
          return (
            <div key={rIdx} className="flex flex-col min-w-[190px]">
              <p className="text-xs font-semibold text-muted-foreground text-center mb-3 uppercase tracking-wider">
                {roundName(rounds.length, rIdx)}
              </p>
              <div className="flex flex-col" style={{ gap, paddingTop }}>
                {round.map((pair, pIdx) => (
                  <div key={pIdx} className="flex flex-col gap-0.5">
                    {pair.map((player, sIdx) => (
                      <div
                        key={sIdx}
                        className={`flex items-center gap-2 px-3 py-2 text-sm border transition-colors ${
                          sIdx === 0 ? 'rounded-t-lg' : 'rounded-b-lg border-t-0'
                        } ${
                          player
                            ? player.confirmed_at
                              ? 'bg-primary/10 border-primary/30 text-foreground'
                              : 'bg-card border-border text-foreground'
                            : 'bg-muted/20 border-dashed border-border/50 text-muted-foreground'
                        }`}
                      >
                        {player ? (
                          <>
                            {player.avatar_url
                              ? <img src={player.avatar_url} className="w-5 h-5 rounded-full flex-shrink-0" />
                              : <div className="w-5 h-5 rounded-full bg-primary/20 flex-shrink-0" />
                            }
                            <span className="truncate max-w-[110px] font-medium">{player.persona_name}</span>
                            {bracketType === 'rating' && (
                              <span className="ml-auto text-xs text-muted-foreground flex-shrink-0">{player.rating ?? 0}</span>
                            )}
                          </>
                        ) : (
                          <span className="text-xs italic opacity-50">TBD</span>
                        )}
                      </div>
                    ))}
                  </div>
                ))}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};

export default BracketView;