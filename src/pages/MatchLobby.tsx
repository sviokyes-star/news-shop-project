import { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate, useSearchParams } from 'react-router-dom';
import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import Icon from '@/components/ui/icon';
import PlayerLink from '@/components/ui/player-link';
import { toast } from '@/hooks/use-toast';
import func2url from '../../backend/func2url.json';

interface SteamUser {
  steamId: string;
  personaName: string;
  avatarUrl: string;
  isAdmin?: boolean;
}

interface Player {
  steam_id: string;
  persona_name: string;
  avatar_url: string | null;
}

interface Message {
  id: number;
  steam_id: string;
  persona_name: string;
  avatar_url: string | null;
  message: string;
  created_at: string;
}

interface Lobby {
  id: number;
  player1_steam_id: string | null;
  player2_steam_id: string | null;
  winner_steam_id: string | null;
  status: string;
  player1_reported_winner: string | null;
  player2_reported_winner: string | null;
  is_dispute: boolean;
  admin_steam_id: string | null;
}

interface LobbyData {
  lobby: Lobby;
  messages: Message[];
  player1: Player | null;
  player2: Player | null;
}

const STATUS_LABELS: Record<string, { label: string; color: string }> = {
  waiting:   { label: 'Ожидание',  color: 'text-yellow-500' },
  active:    { label: 'Идёт',      color: 'text-green-500' },
  dispute:   { label: 'Спор',      color: 'text-orange-500' },
  completed: { label: 'Завершён',  color: 'text-muted-foreground' },
};

export default function MatchLobby() {
  const { tournamentId, roundIndex, matchIndex } = useParams<{
    tournamentId: string;
    roundIndex: string;
    matchIndex: string;
  }>();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const [user, setUser] = useState<SteamUser | null>(null);
  const [data, setData] = useState<LobbyData | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [message, setMessage] = useState('');
  const [isSending, setIsSending] = useState(false);
  const [isReporting, setIsReporting] = useState(false);
  const [showReportPanel, setShowReportPanel] = useState(false);
  const chatEndRef = useRef<HTMLDivElement>(null);
  const pollRef = useRef<ReturnType<typeof setInterval> | null>(null);

  useEffect(() => {
    const saved = localStorage.getItem('steamUser');
    if (saved) setUser(JSON.parse(saved));
  }, []);

  const loadLobby = async () => {
    if (!tournamentId || roundIndex === undefined || matchIndex === undefined) return;
    const steamId = JSON.parse(localStorage.getItem('steamUser') || 'null')?.steamId || '';
    const p1 = searchParams.get('p1') || '';
    const p2 = searchParams.get('p2') || '';
    const url = `${func2url['match-lobby']}?tournament_id=${tournamentId}&round_index=${roundIndex}&match_index=${matchIndex}${steamId ? `&steam_id=${steamId}` : ''}${p1 ? `&player1_steam_id=${p1}` : ''}${p2 ? `&player2_steam_id=${p2}` : ''}`;
    try {
      const res = await fetch(url);
      if (res.status === 403) {
        toast({ title: 'Нет доступа', description: 'Лобби доступно только участникам турнира', variant: 'destructive' });
        navigate(-1);
        return;
      }
      const json = await res.json();
      setData(json);
    } catch {
      // ignore
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadLobby();
    pollRef.current = setInterval(loadLobby, 5000);
    return () => { if (pollRef.current) clearInterval(pollRef.current); };
  }, [tournamentId, roundIndex, matchIndex]);

  useEffect(() => {
    chatEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [data?.messages.length]);

  const sendMessage = async () => {
    if (!message.trim() || !user) return;
    setIsSending(true);
    try {
      await fetch(func2url['match-lobby'], {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-User-Steam-Id': user.steamId },
        body: JSON.stringify({
          action: 'message',
          tournament_id: Number(tournamentId),
          round_index: Number(roundIndex),
          match_index: Number(matchIndex),
          steam_id: user.steamId,
          persona_name: user.personaName,
          avatar_url: user.avatarUrl,
          message: message.trim(),
        }),
      });
      setMessage('');
      await loadLobby();
    } finally {
      setIsSending(false);
    }
  };

  const reportResult = async (winnerSteamId: string) => {
    if (!user) return;
    setIsReporting(true);
    try {
      const res = await fetch(func2url['match-lobby'], {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-User-Steam-Id': user.steamId },
        body: JSON.stringify({
          action: 'report_result',
          tournament_id: Number(tournamentId),
          round_index: Number(roundIndex),
          match_index: Number(matchIndex),
          steam_id: user.steamId,
          winner_steam_id: winnerSteamId,
        }),
      });
      if (res.ok) {
        toast({ title: 'Результат сохранён' });
        setShowReportPanel(false);
        await loadLobby();
      } else {
        const d = await res.json();
        toast({ title: 'Ошибка', description: d.error, variant: 'destructive' });
      }
    } finally {
      setIsReporting(false);
    }
  };

  const isParticipant = data && user && (
    data.lobby.player1_steam_id === user.steamId ||
    data.lobby.player2_steam_id === user.steamId
  );

  const roundNames = ['1/4 финала', 'Полуфинал', 'Финал'];
  const getRoundName = () => {
    const ri = Number(roundIndex);
    if (roundNames[ri]) return roundNames[ri];
    return `Раунд ${ri + 1}`;
  };

  if (isLoading) {
    return (
      <main className="container mx-auto px-6 py-16 flex items-center justify-center min-h-[400px]">
        <Icon name="Loader2" size={40} className="animate-spin text-primary" />
      </main>
    );
  }

  if (!data) {
    return (
      <main className="container mx-auto px-6 py-16 text-center">
        <p className="text-muted-foreground">Лобби не найдено</p>
      </main>
    );
  }

  const { lobby, messages, player1, player2 } = data;
  const statusInfo = STATUS_LABELS[lobby.status] ?? STATUS_LABELS.waiting;

  return (
    <main className="container mx-auto px-6 py-10 max-w-3xl">
      {/* Назад */}
      <Button variant="ghost" onClick={() => navigate(-1)} className="mb-6 gap-2">
        <Icon name="ArrowLeft" size={16} />
        Назад к турниру
      </Button>

      {/* Заголовок */}
      <div className="mb-6 flex items-center justify-between gap-4 flex-wrap">
        <div>
          <h1 className="text-2xl font-bold">Лобби матча</h1>
          <p className="text-muted-foreground text-sm">{getRoundName()}</p>
        </div>
        <div className={`flex items-center gap-2 text-sm font-semibold ${statusInfo.color}`}>
          <Icon name="Circle" size={8} className="fill-current" />
          {statusInfo.label}
        </div>
      </div>

      {/* Участники матча */}
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
            <div className="flex gap-2 text-xs text-muted-foreground">
              <div className={`flex-1 flex items-center gap-1.5 px-3 py-2 rounded-lg border ${lobby.player1_reported_winner ? 'border-primary/40 bg-primary/10 text-foreground' : 'border-border bg-muted/20'}`}>
                <Icon name={lobby.player1_reported_winner ? 'CheckCircle2' : 'Clock'} size={13} className={lobby.player1_reported_winner ? 'text-primary' : 'text-muted-foreground'} />
                <span className="truncate">{player1?.persona_name ?? 'Игрок 1'}</span>
                {lobby.player1_reported_winner && <span className="ml-auto text-xs opacity-70">✓</span>}
              </div>
              <div className={`flex-1 flex items-center gap-1.5 px-3 py-2 rounded-lg border ${lobby.player2_reported_winner ? 'border-primary/40 bg-primary/10 text-foreground' : 'border-border bg-muted/20'}`}>
                <Icon name={lobby.player2_reported_winner ? 'CheckCircle2' : 'Clock'} size={13} className={lobby.player2_reported_winner ? 'text-primary' : 'text-muted-foreground'} />
                <span className="truncate">{player2?.persona_name ?? 'Игрок 2'}</span>
                {lobby.player2_reported_winner && <span className="ml-auto text-xs opacity-70">✓</span>}
              </div>
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

          {/* Кнопка результата — участник, не завершён, не спор или ещё не голосовал */}
          {isParticipant && lobby.status !== 'completed' && !lobby.is_dispute && (() => {
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
              <div className="space-y-2">
                <p className="text-sm font-medium text-center">Кто победил?</p>
                <div className="flex gap-3">
                  {[player1, player2].map(p => p && (
                    <Button key={p.steam_id} className="flex-1 gap-2" disabled={isReporting} onClick={() => reportResult(p.steam_id)}>
                      {p.avatar_url && <img src={p.avatar_url} className="w-5 h-5 rounded-full" />}
                      {p.persona_name}
                    </Button>
                  ))}
                </div>
                <Button variant="ghost" size="sm" className="w-full" onClick={() => setShowReportPanel(false)}>Отмена</Button>
              </div>
            );
          })()}

          {/* Кнопка решения спора — только для администратора */}
          {lobby.is_dispute && user && (() => {
            const isAdmin = user.isAdmin;
            if (!isAdmin) return null;
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
                          if (res.ok) { toast({ title: 'Спор разрешён' }); setShowReportPanel(false); await loadLobby(); }
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

      {/* Чат */}
      <Card className="border-border bg-card/60 flex flex-col" style={{ height: 400 }}>
        <div className="px-4 py-3 border-b border-border flex items-center gap-2">
          <Icon name="MessageSquare" size={16} className="text-primary" />
          <span className="font-semibold text-sm">Чат матча</span>
        </div>

        <div className="flex-1 overflow-y-auto px-4 py-3 space-y-3">
          {messages.length === 0 ? (
            <p className="text-center text-muted-foreground text-sm mt-8">Сообщений пока нет</p>
          ) : messages.map(msg => (
            <div key={msg.id} className={`flex gap-2 ${msg.steam_id === user?.steamId ? 'flex-row-reverse' : ''}`}>
              {msg.avatar_url
                ? <img src={msg.avatar_url} className="w-7 h-7 rounded-full flex-shrink-0 mt-0.5" />
                : <div className="w-7 h-7 rounded-full bg-primary/20 flex-shrink-0 mt-0.5" />
              }
              <div className={`max-w-[75%] ${msg.steam_id === user?.steamId ? 'items-end' : 'items-start'} flex flex-col gap-0.5`}>
                <span className="text-xs text-muted-foreground px-1">{msg.persona_name}</span>
                <div className={`px-3 py-2 rounded-xl text-sm ${
                  msg.steam_id === user?.steamId
                    ? 'bg-primary text-primary-foreground rounded-tr-none'
                    : 'bg-muted/60 rounded-tl-none'
                }`}>
                  {msg.message}
                </div>
              </div>
            </div>
          ))}
          <div ref={chatEndRef} />
        </div>

        <div className="px-3 py-3 border-t border-border">
          {user ? (
            <div className="flex gap-2">
              <Input
                value={message}
                onChange={e => setMessage(e.target.value)}
                placeholder="Написать сообщение..."
                className="flex-1 h-9 text-sm"
                onKeyDown={e => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); sendMessage(); } }}
                disabled={isSending}
              />
              <Button size="sm" className="h-9 px-3" onClick={sendMessage} disabled={isSending || !message.trim()}>
                <Icon name="Send" size={15} />
              </Button>
            </div>
          ) : (
            <p className="text-center text-xs text-muted-foreground">Войдите через Steam чтобы писать в чат</p>
          )}
        </div>
      </Card>
    </main>
  );
}