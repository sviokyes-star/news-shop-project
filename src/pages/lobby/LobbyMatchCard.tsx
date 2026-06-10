import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import Icon from '@/components/ui/icon';
import PlayerLink from '@/components/ui/player-link';
import { toast } from '@/hooks/use-toast';
import func2url from '../../../backend/func2url.json';
import { Lobby, Player, SteamUser } from './types';

interface LobbyMatchCardProps {
  lobby: Lobby;
  player1: Player | null;
  player2: Player | null;
  user: SteamUser | null;
  isParticipant: boolean;
  isTournamentAdmin: boolean;
  showReportPanel: boolean;
  setShowReportPanel: (v: boolean) => void;
  screenshotFile: File | null;
  screenshotPreview: string | null;
  isReporting: boolean;
  isUploadingScreenshot: boolean;
  tournamentId: string;
  roundIndex: string;
  matchIndex: string;
  onScreenshotChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
  onClearScreenshot: () => void;
  onUploadScreenshot: (winnerSteamId: string) => void;
  setIsReporting: (v: boolean) => void;
  onReloadLobby: () => void;
}

export default function LobbyMatchCard({
  lobby, player1, player2, user, isParticipant, isTournamentAdmin,
  showReportPanel, setShowReportPanel,
  screenshotFile, screenshotPreview,
  isReporting, isUploadingScreenshot,
  tournamentId, roundIndex, matchIndex,
  onScreenshotChange, onClearScreenshot, onUploadScreenshot,
  setIsReporting, onReloadLobby,
}: LobbyMatchCardProps) {
  return (
    <Card className="p-5 mb-6 border-border bg-card/60">
      <div className="flex items-center justify-between gap-4">
        {/* Игрок 1 */}
        <div className="flex flex-col items-center gap-2 flex-1">
          {player1 ? (
            <>
              <div className="relative">
                {player1.avatar_url
                  ? <img src={player1.avatar_url} className="w-16 h-16 rounded-xl border-2 border-border" />
                  : <div className="w-16 h-16 rounded-xl bg-primary/20 flex items-center justify-center"><Icon name="User" size={28} /></div>
                }
                {lobby.winner_steam_id === player1.steam_id && (
                  <div className="absolute -top-2 -right-2 w-6 h-6 bg-yellow-500 rounded-full flex items-center justify-center">
                    <Icon name="Trophy" size={12} className="text-white" />
                  </div>
                )}
              </div>
              <PlayerLink steamId={player1.steam_id} name={player1.persona_name} className="font-semibold text-sm" />
            </>
          ) : (
            <div className="w-16 h-16 rounded-xl bg-muted/30 border border-dashed border-border flex items-center justify-center">
              <Icon name="User" size={24} className="text-muted-foreground opacity-30" />
            </div>
          )}
        </div>

        <div className="text-2xl font-bold text-muted-foreground">VS</div>

        {/* Игрок 2 */}
        <div className="flex flex-col items-center gap-2 flex-1">
          {player2 ? (
            <>
              <div className="relative">
                {player2.avatar_url
                  ? <img src={player2.avatar_url} className="w-16 h-16 rounded-xl border-2 border-border" />
                  : <div className="w-16 h-16 rounded-xl bg-primary/20 flex items-center justify-center"><Icon name="User" size={28} /></div>
                }
                {lobby.winner_steam_id === player2.steam_id && (
                  <div className="absolute -top-2 -right-2 w-6 h-6 bg-yellow-500 rounded-full flex items-center justify-center">
                    <Icon name="Trophy" size={12} className="text-white" />
                  </div>
                )}
              </div>
              <PlayerLink steamId={player2.steam_id} name={player2.persona_name} className="font-semibold text-sm" />
            </>
          ) : (
            <div className="w-16 h-16 rounded-xl bg-muted/30 border border-dashed border-border flex items-center justify-center">
              <Icon name="User" size={24} className="text-muted-foreground opacity-30" />
            </div>
          )}
        </div>
      </div>

      <div className="mt-4 pt-4 border-t border-border space-y-3">

        {/* Статус голосов */}
        {(lobby.player1_reported_winner || lobby.player2_reported_winner) && lobby.status !== 'completed' && (
          <div className="flex flex-col gap-1.5 text-xs">
            {[
              { voterName: player1?.persona_name ?? 'Игрок 1', votedFor: lobby.player1_reported_winner },
              { voterName: player2?.persona_name ?? 'Игрок 2', votedFor: lobby.player2_reported_winner },
            ].map(({ voterName, votedFor }, i) => {
              const votedName = votedFor ? ([player1, player2].find(p => p?.steam_id === votedFor)?.persona_name ?? votedFor) : null;
              return (
                <div key={i} className={`flex items-center gap-1.5 px-3 py-2 rounded-lg border ${votedFor ? 'border-primary/40 bg-primary/10 text-foreground' : 'border-border bg-muted/20 text-muted-foreground'}`}>
                  <Icon name={votedFor ? 'CheckCircle2' : 'Clock'} size={13} className={votedFor ? 'text-primary' : 'text-muted-foreground'} />
                  <span className="font-medium">{voterName}</span>
                  {votedFor && <>
                    <span className="text-muted-foreground mx-1">→</span>
                    <span className="font-semibold text-primary">{votedName}</span>
                  </>}
                  {!votedFor && <span className="ml-1 opacity-60">не голосовал</span>}
                </div>
              );
            })}
          </div>
        )}

        {/* Баннер спора */}
        {lobby.is_dispute && (
          <div className="flex items-start gap-3 px-4 py-3 rounded-xl bg-orange-500/10 border border-orange-500/30">
            <Icon name="AlertTriangle" size={18} className="text-orange-500 flex-shrink-0 mt-0.5" />
            <div>
              <p className="text-sm font-semibold text-orange-500">Открыт спор</p>
              <p className="text-xs text-muted-foreground mt-0.5">Игроки указали разных победителей. Ожидается решение администратора.</p>
            </div>
          </div>
        )}

        {/* Ожидание соперника */}
        {isParticipant && (!lobby.player1_steam_id || !lobby.player2_steam_id) && lobby.status !== 'completed' && (
          <div className="flex items-center gap-2 px-4 py-3 rounded-xl bg-muted/30 border border-border text-sm text-muted-foreground">
            <Icon name="Clock" size={15} />
            Ожидаем соперника...
          </div>
        )}

        {/* Кнопка результата — участник, не завершён, не спор, оба игрока определены */}
        {isParticipant && lobby.status !== 'completed' && !lobby.is_dispute && lobby.player1_steam_id && lobby.player2_steam_id && (() => {
          const myVote = user?.steamId === lobby.player1_steam_id
            ? lobby.player1_reported_winner
            : lobby.player2_reported_winner;
          if (myVote) return (
            <p className="text-center text-sm text-muted-foreground py-1">
              Вы указали победителем: <span className="font-semibold text-foreground">{[player1, player2].find(p => p?.steam_id === myVote)?.persona_name ?? myVote}</span>
            </p>
          );
          return !showReportPanel ? (
            <Button variant="outline" className="w-full gap-2" onClick={() => setShowReportPanel(true)}>
              <Icon name="Flag" size={16} />
              Сообщить результат
            </Button>
          ) : (
            <div className="space-y-3">
              <div>
                <p className="text-sm font-medium mb-2">Скриншот победного экрана <span className="text-destructive">*</span></p>
                {screenshotPreview ? (
                  <div className="relative">
                    <img src={screenshotPreview} className="w-full max-h-48 object-cover rounded-lg border border-border" />
                    <button
                      className="absolute top-2 right-2 w-6 h-6 bg-background/80 rounded-full flex items-center justify-center hover:bg-background"
                      onClick={onClearScreenshot}
                    >
                      <Icon name="X" size={12} />
                    </button>
                  </div>
                ) : (
                  <label className="flex flex-col items-center gap-2 p-4 border border-dashed border-border rounded-lg cursor-pointer hover:border-primary/50 transition-colors">
                    <Icon name="ImagePlus" size={24} className="text-muted-foreground" />
                    <span className="text-sm text-muted-foreground">Нажмите чтобы выбрать файл</span>
                    <input type="file" accept="image/*" className="hidden" onChange={onScreenshotChange} />
                  </label>
                )}
              </div>

              <p className="text-sm font-medium text-center">Кто победил?</p>
              <div className="flex gap-3">
                {[player1, player2].map(p => p && (
                  <Button
                    key={p.steam_id}
                    className="flex-1 gap-2"
                    disabled={isReporting || isUploadingScreenshot || !screenshotFile}
                    onClick={() => onUploadScreenshot(p.steam_id)}
                  >
                    {(isReporting || isUploadingScreenshot) ? <Icon name="Loader2" size={14} className="animate-spin" /> : p.avatar_url && <img src={p.avatar_url} className="w-5 h-5 rounded-full" />}
                    {p.persona_name}
                  </Button>
                ))}
              </div>
              {!screenshotFile && <p className="text-xs text-muted-foreground text-center">Загрузите скриншот перед отправкой результата</p>}
              <Button variant="ghost" size="sm" className="w-full" onClick={() => { setShowReportPanel(false); onClearScreenshot(); }}>Отмена</Button>
            </div>
          );
        })()}

        {/* Кнопка решения спора — только для администратора турнира */}
        {lobby.is_dispute && isTournamentAdmin && user && (() => {
          return !showReportPanel ? (
            <Button variant="destructive" className="w-full gap-2" onClick={() => setShowReportPanel(true)}>
              <Icon name="Gavel" size={16} />
              Разрешить спор
            </Button>
          ) : (
            <div className="space-y-2">
              <p className="text-sm font-semibold text-center text-orange-500">Выберите победителя (решение администратора)</p>
              <div className="flex gap-3">
                {[player1, player2].map(p => p && (
                  <Button key={p.steam_id} variant="outline" className="flex-1 gap-2 border-orange-500/40 hover:bg-orange-500/10"
                    disabled={isReporting}
                    onClick={async () => {
                      setIsReporting(true);
                      try {
                        const res = await fetch(func2url['match-lobby'], {
                          method: 'POST',
                          headers: { 'Content-Type': 'application/json', 'X-User-Steam-Id': user.steamId },
                          body: JSON.stringify({
                            action: 'resolve_dispute',
                            tournament_id: Number(tournamentId),
                            round_index: Number(roundIndex),
                            match_index: Number(matchIndex),
                            steam_id: user.steamId,
                            winner_steam_id: p.steam_id,
                          }),
                        });
                        if (res.ok) { toast({ title: 'Спор разрешён' }); setShowReportPanel(false); onReloadLobby(); }
                        else { const d = await res.json(); toast({ title: 'Ошибка', description: d.error, variant: 'destructive' }); }
                      } finally { setIsReporting(false); }
                    }}
                  >
                    {p.avatar_url && <img src={p.avatar_url} className="w-5 h-5 rounded-full" />}
                    {p.persona_name}
                  </Button>
                ))}
              </div>
              <Button variant="ghost" size="sm" className="w-full" onClick={() => setShowReportPanel(false)}>Отмена</Button>
            </div>
          );
        })()}

        {/* Победитель */}
        {lobby.status === 'completed' && lobby.winner_steam_id && (
          <div className="flex items-center justify-center gap-2 text-sm font-semibold text-yellow-500 py-1">
            <Icon name="Trophy" size={16} />
            Победитель: {[player1, player2].find(p => p?.steam_id === lobby.winner_steam_id)?.persona_name ?? lobby.winner_steam_id}
          </div>
        )}
      </div>
    </Card>
  );
}