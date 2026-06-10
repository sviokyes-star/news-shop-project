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

// Высота одного слота (строки игрока) в px
const SLOT_H = 36;
// Высота матча (2 слота)
const MATCH_H = SLOT_H * 2;
// Минимальный отступ между матчами в первом раунде
const BASE_GAP = 12;
// Ширина колонки
const COL_W = 200;
// Горизонтальный отступ между колонками
const COL_GAP = 48;

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

  // Строим структуру раундов
  const firstRound: (Participant | null)[][] = [];
  for (let i = 0; i < slots.length; i += 2) firstRound.push([slots[i], slots[i + 1]]);

  const rounds: (Participant | null)[][][] = [firstRound];
  let rs = firstRound.length;
  while (rs > 1) {
    rs = Math.ceil(rs / 2);
    rounds.push(Array.from({ length: rs }, () => [null, null]));
  }

  // Вычисляем Y-позицию каждого матча
  // В раунде 0: матчи расположены друг за другом с BASE_GAP
  // В раунде r: каждый матч центрирован между двумя матчами раунда r-1
  const matchStep0 = MATCH_H + BASE_GAP; // шаг между матчами в раунде 0

  function matchY(roundIdx: number, matchIdx: number): number {
    if (roundIdx === 0) return matchIdx * matchStep0;
    // Шаг удваивается каждый раунд
    const step = matchStep0 * Math.pow(2, roundIdx);
    const offset = (step - matchStep0) / 2; // сдвиг первого матча
    return offset + matchIdx * step;
  }

  const totalRounds = rounds.length;
  const totalHeight = rounds[0].length * matchStep0 - BASE_GAP;

  const roundName = (total: number, idx: number) => {
    const fromEnd = total - 1 - idx;
    if (fromEnd === 0) return 'Финал';
    if (fromEnd === 1) return 'Полуфинал';
    if (fromEnd === 2) return '1/4 финала';
    return `Раунд ${idx + 1}`;
  };

  const totalWidth = totalRounds * COL_W + (totalRounds - 1) * COL_GAP;

  return (
    <div className="overflow-x-auto pb-4">
      <div style={{ width: totalWidth, minHeight: totalHeight + MATCH_H }} className="relative">
        {rounds.map((round, rIdx) => (
          <div
            key={rIdx}
            style={{
              position: 'absolute',
              left: rIdx * (COL_W + COL_GAP),
              top: 0,
              width: COL_W,
            }}
          >
            {/* Заголовок раунда */}
            <div className="text-xs font-semibold text-muted-foreground text-center mb-0 uppercase tracking-wider"
              style={{ position: 'absolute', top: -24, width: COL_W }}>
              {roundName(totalRounds, rIdx)}
            </div>

            {round.map((pair, pIdx) => {
              const top = matchY(rIdx, pIdx);
              return (
                <div key={pIdx} style={{ position: 'absolute', top, width: COL_W }}>
                  {pair.map((player, sIdx) => (
                    <div
                      key={sIdx}
                      style={{ height: SLOT_H }}
                      className={`flex items-center gap-2 px-3 text-sm border transition-colors ${
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
                          <span className="truncate font-medium" style={{ maxWidth: 110 }}>{player.persona_name}</span>
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
              );
            })}
          </div>
        ))}
      </div>
    </div>
  );
};

export default BracketView;
