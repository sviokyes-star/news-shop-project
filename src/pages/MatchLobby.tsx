import { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate, useSearchParams } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import Icon from '@/components/ui/icon';
import { toast } from '@/hooks/use-toast';
import func2url from '../../backend/func2url.json';
import LobbyMatchCard from './lobby/LobbyMatchCard';
import LobbyChat from './lobby/LobbyChat';
import { SteamUser, LobbyData, STATUS_LABELS, TournamentAdmin } from './lobby/types';

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
  const [screenshotFile, setScreenshotFile] = useState<File | null>(null);
  const [screenshotPreview, setScreenshotPreview] = useState<string | null>(null);
  const [isUploadingScreenshot, setIsUploadingScreenshot] = useState(false);
  const [tournamentAdmins, setTournamentAdmins] = useState<TournamentAdmin[]>([]);
  const pollRef = useRef<ReturnType<typeof setInterval> | null>(null);

  useEffect(() => {
    const saved = localStorage.getItem('steamUser');
    if (saved) setUser(JSON.parse(saved));
  }, []);

  useEffect(() => {
    if (!tournamentId) return;
    fetch(`${func2url.tournaments}?tournament_id=${tournamentId}`)
      .then(r => r.json())
      .then(d => setTournamentAdmins(d.tournament_admins || []))
      .catch(() => {});
  }, [tournamentId]);

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

  const handleScreenshotChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setScreenshotFile(file);
    const reader = new FileReader();
    reader.onload = ev => setScreenshotPreview(ev.target?.result as string);
    reader.readAsDataURL(file);
  };

  const clearScreenshot = () => {
    setScreenshotFile(null);
    setScreenshotPreview(null);
  };

  const uploadScreenshot = async (winnerSteamId: string) => {
    if (!user || !screenshotFile) return;
    setIsUploadingScreenshot(true);
    try {
      const reader = new FileReader();
      const b64 = await new Promise<string>((resolve) => {
        reader.onload = ev => {
          const result = ev.target?.result as string;
          resolve(result.split(',')[1]);
        };
        reader.readAsDataURL(screenshotFile);
      });
      await fetch(func2url['match-lobby'], {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-User-Steam-Id': user.steamId },
        body: JSON.stringify({
          action: 'upload_screenshot',
          tournament_id: Number(tournamentId),
          round_index: Number(roundIndex),
          match_index: Number(matchIndex),
          steam_id: user.steamId,
          persona_name: user.personaName,
          avatar_url: user.avatarUrl,
          image_b64: b64,
          content_type: screenshotFile.type || 'image/png',
        }),
      });
    } finally {
      setIsUploadingScreenshot(false);
    }
    await reportResult(winnerSteamId);
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
  const isParticipant = !!(user && (
    lobby.player1_steam_id === user.steamId ||
    lobby.player2_steam_id === user.steamId
  ));
  const isTournamentAdmin = !!(user && tournamentAdmins.some(a => a.steam_id === user.steamId));

  return (
    <main className="container mx-auto px-6 py-10 max-w-3xl">
      <Button variant="ghost" onClick={() => navigate(-1)} className="mb-6 gap-2">
        <Icon name="ArrowLeft" size={16} />
        Назад к турниру
      </Button>

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

      <LobbyMatchCard
        lobby={lobby}
        player1={player1}
        player2={player2}
        user={user}
        isParticipant={isParticipant}
        isTournamentAdmin={isTournamentAdmin}
        showReportPanel={showReportPanel}
        setShowReportPanel={setShowReportPanel}
        screenshotFile={screenshotFile}
        screenshotPreview={screenshotPreview}
        isReporting={isReporting}
        isUploadingScreenshot={isUploadingScreenshot}
        tournamentId={tournamentId!}
        roundIndex={roundIndex!}
        matchIndex={matchIndex!}
        onScreenshotChange={handleScreenshotChange}
        onClearScreenshot={clearScreenshot}
        onUploadScreenshot={uploadScreenshot}
        setIsReporting={setIsReporting}
        onReloadLobby={loadLobby}
      />

      <LobbyChat
        messages={messages}
        user={user}
        message={message}
        isSending={isSending}
        onMessageChange={setMessage}
        onSend={sendMessage}
        tournamentAdmins={tournamentAdmins}
      />
    </main>
  );
}