import { Card } from '@/components/ui/card';
import Icon from '@/components/ui/icon';
import PlayerLink from '@/components/ui/player-link';

interface BracketMatch {
  id: number;
  round_number: number;
  match_number: number;
  player1_steam_id: string | null;
  player2_steam_id: string | null;
  player1_name: string | null;
  player2_name: string | null;
  player1_avatar: string | null;
  player2_avatar: string | null;
  winner_steam_id: string | null;
  player1_score: number;
  player2_score: number;
  status: string;
}

interface TournamentBracketProps {
  bracket: BracketMatch[];
}

const TournamentBracket = ({ bracket }: TournamentBracketProps) => {
  if (!bracket || bracket.length === 0) {
    return null;
  }

  const rounds = Array.from(new Set(bracket.map(m => m.round_number))).sort();
  
  const getRoundName = (roundNum: number, totalRounds: number) => {
    const roundsLeft = totalRounds - roundNum + 1;
    if (roundsLeft === 1) return 'Финал';
    if (roundsLeft === 2) return 'Полуфинал';
    if (roundsLeft === 3) return 'Четвертьфинал';
    return `Раунд ${roundNum}`;
  };

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-3xl font-bold">Турнирная сетка</h2>
        <p className="text-muted-foreground mt-1">Single Elimination</p>
      </div>

      <div className="overflow-x-auto">
        <div className="flex gap-8 pb-4" style={{ minWidth: `${rounds.length * 350}px` }}>
          {rounds.map(roundNum => {
            const roundMatches = bracket.filter(m => m.round_number === roundNum);
            
            return (
              <div key={roundNum} className="flex-1 min-w-[320px]">
                <h3 className="text-xl font-bold mb-4 text-center">
                  {getRoundName(roundNum, rounds.length)}
                </h3>
                
                <div className="space-y-4">
                  {roundMatches.map(match => (
                    <Card 
                      key={match.id}
                      className="p-4 hover:border-primary/50 transition-all"
                    >
                      <div className="space-y-3">
                        <div className="flex items-center justify-between text-xs text-muted-foreground">
                          <span>Матч #{match.match_number + 1}</span>
                          {match.status === 'completed' && (
                            <span className="flex items-center gap-1 text-green-500">
                              <Icon name="CheckCircle2" size={12} />
                              Завершён
                            </span>
                          )}
                          {match.status === 'pending' && (
                            <span className="flex items-center gap-1">
                              <Icon name="Clock" size={12} />
                              Ожидание
                            </span>
                          )}
                        </div>

                        <div className="space-y-2">
                          {/* Player 1 */}
                          <div 
                            className={`flex items-center gap-3 p-2 rounded ${
                              match.winner_steam_id === match.player1_steam_id 
                                ? 'bg-green-500/10 border border-green-500/30' 
                                : 'bg-secondary/50'
                            }`}
                          >
                            {match.player1_steam_id && match.player1_name ? (
                              <PlayerLink steamId={match.player1_steam_id} name={match.player1_name} showAvatar avatarUrl={match.player1_avatar} avatarSize={8} className="flex-1" />
                            ) : (
                              <>
                                <div className="w-8 h-8 rounded-full bg-muted flex items-center justify-center">
                                  <Icon name="User" size={16} />
                                </div>
                                <span className="flex-1 font-medium text-sm text-muted-foreground">TBD</span>
                              </>
                            )}
                            <span className="text-lg font-bold">{match.player1_score}</span>
                          </div>

                          {/* Player 2 */}
                          <div 
                            className={`flex items-center gap-3 p-2 rounded ${
                              match.winner_steam_id === match.player2_steam_id 
                                ? 'bg-green-500/10 border border-green-500/30' 
                                : 'bg-secondary/50'
                            }`}
                          >
                            {match.player2_steam_id && match.player2_name ? (
                              <PlayerLink steamId={match.player2_steam_id} name={match.player2_name} showAvatar avatarUrl={match.player2_avatar} avatarSize={8} className="flex-1" />
                            ) : (
                              <>
                                <div className="w-8 h-8 rounded-full bg-muted flex items-center justify-center">
                                  <Icon name="User" size={16} />
                                </div>
                                <span className="flex-1 font-medium text-sm text-muted-foreground">TBD</span>
                              </>
                            )}
                            <span className="text-lg font-bold">{match.player2_score}</span>
                          </div>
                        </div>
                      </div>
                    </Card>
                  ))}
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
};

export default TournamentBracket;