import { Card } from '@/components/ui/card';
import Icon from '@/components/ui/icon';
import { Participant } from './types';

interface BracketViewProps {
  participants: Participant[];
  maxParticipants: number;
}

const BracketView = ({ participants, maxParticipants }: BracketViewProps) => {
  if (participants.length < 2) {
    return (
      <Card className="p-12 text-center">
        <Icon name="GitBranch" size={48} className="mx-auto text-muted-foreground mb-4 opacity-40" />
        <p className="text-muted-foreground">Сетка будет построена после заполнения участников</p>
      </Card>
    );
  }

  const confirmed = participants.filter(p => p.confirmed_at);
  const pool = confirmed.length >= 2 ? confirmed : participants;

  const size = Math.pow(2, Math.ceil(Math.log2(Math.max(pool.length, 2))));
  const slots: (Participant | null)[] = [...pool];
  while (slots.length < size) slots.push(null);

  const rounds: (Participant | null)[][][] = [];
  const current: (Participant | null)[][] = [];
  for (let i = 0; i < slots.length; i += 2) {
    current.push([slots[i], slots[i + 1]]);
  }
  rounds.push(current);

  let roundSize = current.length;
  while (roundSize > 1) {
    roundSize = Math.ceil(roundSize / 2);
    const nextRound: (Participant | null)[][] = [];
    for (let i = 0; i < roundSize; i++) nextRound.push([null, null]);
    rounds.push(nextRound);
  }

  const roundNames = (totalRounds: number, idx: number) => {
    const fromEnd = totalRounds - 1 - idx;
    if (fromEnd === 0) return 'Финал';
    if (fromEnd === 1) return 'Полуфинал';
    if (fromEnd === 2) return '1/4 финала';
    return `Раунд ${idx + 1}`;
  };

  return (
    <div className="overflow-x-auto pb-4">
      <div className="flex gap-6 min-w-max">
        {rounds.map((round, rIdx) => (
          <div key={rIdx} className="flex flex-col gap-2 min-w-[180px]">
            <p className="text-xs font-semibold text-muted-foreground text-center mb-2 uppercase tracking-wider">
              {roundNames(rounds.length, rIdx)}
            </p>
            <div className="flex flex-col justify-around h-full gap-4">
              {round.map((pair, pIdx) => (
                <div key={pIdx} className="flex flex-col gap-1">
                  {pair.map((player, sIdx) => (
                    <div
                      key={sIdx}
                      className={`flex items-center gap-2 px-3 py-2 rounded-lg border text-sm transition-colors ${
                        player
                          ? player.confirmed_at
                            ? 'bg-primary/10 border-primary/40 text-foreground'
                            : 'bg-card border-border text-foreground'
                          : 'bg-muted/30 border-dashed border-border text-muted-foreground'
                      }`}
                    >
                      {player ? (
                        <>
                          {player.avatar_url ? (
                            <img src={player.avatar_url} className="w-5 h-5 rounded-full flex-shrink-0" />
                          ) : (
                            <div className="w-5 h-5 rounded-full bg-primary/20 flex-shrink-0" />
                          )}
                          <span className="truncate max-w-[120px] font-medium">{player.persona_name}</span>
                          {player.confirmed_at && <Icon name="CheckCircle2" size={12} className="text-green-500 flex-shrink-0 ml-auto" />}
                        </>
                      ) : (
                        <span className="text-xs italic">TBD</span>
                      )}
                    </div>
                  ))}
                </div>
              ))}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

export default BracketView;
