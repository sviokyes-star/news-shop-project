import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import Icon from '@/components/ui/icon';
import func2url from '../../backend/func2url.json';
import TournamentInfo from '@/components/tournament/TournamentInfo';
import CountdownTimer from '@/components/tournament/CountdownTimer';
import TournamentActions from '@/components/tournament/TournamentActions';
import TournamentUserMatch from '@/components/tournament/TournamentUserMatch';
import TournamentTabs from '@/components/tournament/TournamentTabs';
import { TournamentDetail as TournamentDetailType, SteamUser } from '@/components/tournament/types';
import { getTimeUntilStart, findUserMatch } from '@/components/tournament/utils';
import { useTournamentActions } from '@/components/tournament/useTournamentActions';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog';

const TournamentDetail = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [tournament, setTournament] = useState<TournamentDetailType | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [user, setUser] = useState<SteamUser | null>(null);
  const [, setTick] = useState(0);
  const [showUnregisterDialog, setShowUnregisterDialog] = useState(false);
  const [activeTab, setActiveTab] = useState<'rules' | 'participants' | 'bracket' | 'prizes'>('rules');

  useEffect(() => {
    const timer = setInterval(() => { setTick(t => t + 1); }, 1000);
    return () => clearInterval(timer);
  }, []);

  const loadTournamentDetails = async () => {
    try {
      const response = await fetch(`${func2url.tournaments}?tournament_id=${id}`);
      const data = await response.json();
      setTournament(data);
    } catch (error) {
      console.error('Failed to load tournament details:', error);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    const savedUser = localStorage.getItem('steamUser');
    if (savedUser) setUser(JSON.parse(savedUser));

    loadTournamentDetails();

    const params = new URLSearchParams(window.location.search);
    const claimedId = params.get('openid.claimed_id');
    if (claimedId) {
      const verifyParams = new URLSearchParams();
      params.forEach((value, key) => { verifyParams.append(key, value); });
      verifyParams.append('mode', 'verify');
      fetch(`${func2url['steam-auth']}?${verifyParams.toString()}`)
        .then(res => res.json())
        .then(data => {
          if (data.steamId) {
            setUser(data);
            localStorage.setItem('steamUser', JSON.stringify(data));
            window.history.replaceState({}, document.title, window.location.pathname);
          }
        })
        .catch(error => console.error('Steam auth failed:', error));
    }
  }, [id]);

  const { isRegistering, isUnregistering, isConfirming, handleRegister, confirmUnregister, handleConfirmParticipation } =
    useTournamentActions(id, user, loadTournamentDetails);

  if (isLoading) {
    return (
      <main className="container mx-auto px-6 py-16">
        <div className="flex items-center justify-center min-h-[400px]">
          <div className="text-center space-y-4">
            <Icon name="Loader2" size={48} className="animate-spin text-primary mx-auto" />
            <p className="text-muted-foreground">Загрузка турнира...</p>
          </div>
        </div>
      </main>
    );
  }

  if (!tournament) {
    return (
      <main className="container mx-auto px-6 py-16">
        <div className="flex items-center justify-center min-h-[400px]">
          <div className="text-center space-y-4">
            <Icon name="AlertCircle" size={48} className="text-destructive mx-auto" />
            <h2 className="text-2xl font-bold">Турнир не найден</h2>
            <p className="text-muted-foreground">Такого турнира не существует или он был удален</p>
            <Button onClick={() => navigate('/tournaments')} className="gap-2 mt-4">
              <Icon name="ArrowLeft" size={16} />
              К списку турниров
            </Button>
          </div>
        </div>
      </main>
    );
  }

  const isRegistered = tournament.participants.some(p => p.steam_id === user?.steamId);
  const isFull = tournament.participants_count >= tournament.max_participants;
  const tournamentStarted = ['active', 'ongoing', 'completed'].includes(tournament.status)
    || new Date(tournament.start_date).getTime() <= Date.now();
  const userMatch = (tournamentStarted && isRegistered && user)
    ? findUserMatch(tournament.participants, tournament.bracket_type || 'random', user.steamId, tournament.match_lobbies || [])
    : null;

  return (
    <main className="container mx-auto px-6 py-16">
      <Button variant="ghost" onClick={() => navigate('/tournaments')} className="mb-6 gap-2">
        <Icon name="ArrowLeft" size={16} />
        Назад к турнирам
      </Button>

      <div className="space-y-8">
        <TournamentInfo tournament={tournament} />

        {tournament.status === 'upcoming' && getTimeUntilStart(tournament.start_date) && (
          <CountdownTimer startDate={tournament.start_date} />
        )}

        {tournamentStarted && isRegistered ? (
          <TournamentUserMatch tournament={tournament} userMatch={userMatch} />
        ) : (
          <TournamentActions
            tournament={tournament}
            user={user}
            isRegistered={isRegistered}
            isFull={isFull}
            isRegistering={isRegistering}
            isUnregistering={isUnregistering}
            isConfirming={isConfirming}
            onRegister={handleRegister}
            onUnregister={() => setShowUnregisterDialog(true)}
            onConfirm={() => handleConfirmParticipation(tournament.id)}
          />
        )}

        <TournamentTabs
          tournament={tournament}
          activeTab={activeTab}
          onTabChange={setActiveTab}
        />
      </div>

      <AlertDialog open={showUnregisterDialog} onOpenChange={setShowUnregisterDialog}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Отменить регистрацию?</AlertDialogTitle>
            <AlertDialogDescription>
              Вы уверены, что хотите отменить регистрацию на турнир? Это действие можно отменить, зарегистрировавшись снова.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Отмена</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => { setShowUnregisterDialog(false); confirmUnregister(); }}
              className="bg-destructive hover:bg-destructive/90"
            >
              Отменить регистрацию
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </main>
  );
};

export default TournamentDetail;
