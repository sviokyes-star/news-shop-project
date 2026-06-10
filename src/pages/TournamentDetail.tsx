import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import Icon from '@/components/ui/icon';
import func2url from '../../backend/func2url.json';
import TournamentInfo from '@/components/tournament/TournamentInfo';
import CountdownTimer from '@/components/tournament/CountdownTimer';
import TournamentActions from '@/components/tournament/TournamentActions';
import ParticipantsList from '@/components/tournament/ParticipantsList';
import BracketView from '@/components/tournament/BracketView';
import { TournamentDetail as TournamentDetailType, SteamUser } from '@/components/tournament/types';
import { getTimeUntilStart } from '@/components/tournament/utils';
import { toast } from '@/hooks/use-toast';
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
  const [isRegistering, setIsRegistering] = useState(false);
  const [isUnregistering, setIsUnregistering] = useState(false);
  const [isConfirming, setIsConfirming] = useState(false);
  const [, setTick] = useState(0);
  const [showUnregisterDialog, setShowUnregisterDialog] = useState(false);
  const [activeTab, setActiveTab] = useState<'rules' | 'participants' | 'bracket' | 'prizes'>('rules');

  useEffect(() => {
    const timer = setInterval(() => {
      setTick(t => t + 1);
    }, 1000);
    return () => clearInterval(timer);
  }, []);

  useEffect(() => {
    const savedUser = localStorage.getItem('steamUser');
    if (savedUser) {
      setUser(JSON.parse(savedUser));
    }

    loadTournamentDetails();

    const params = new URLSearchParams(window.location.search);
    const claimedId = params.get('openid.claimed_id');
    
    if (claimedId) {
      const verifyParams = new URLSearchParams();
      params.forEach((value, key) => {
        verifyParams.append(key, value);
      });
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

  const loadTournamentDetails = async () => {
    try {
      const response = await fetch(
        `${func2url.tournaments}?tournament_id=${id}`
      );
      const data = await response.json();
      setTournament(data);
    } catch (error) {
      console.error('Failed to load tournament details:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleRegister = async () => {
    if (!user) {
      toast({
        title: "Требуется авторизация",
        description: "Войдите через Steam для регистрации на турнир",
        variant: "destructive"
      });
      return;
    }

    setIsRegistering(true);

    try {
      const response = await fetch(func2url.tournaments, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          tournament_id: Number(id),
          steam_id: user.steamId,
          persona_name: user.personaName,
          avatar_url: user.avatarUrl
        })
      });

      const data = await response.json();

      if (response.ok) {
        toast({
          title: "Успешно!",
          description: "Регистрация успешна! Увидимся на турнире!"
        });
        await loadTournamentDetails();
      } else {
        toast({
          title: "Ошибка",
          description: data.error || 'Ошибка регистрации',
          variant: "destructive"
        });
      }
    } catch (error) {
      console.error('Registration failed:', error);
      toast({
        title: "Ошибка",
        description: "Ошибка при регистрации",
        variant: "destructive"
      });
    } finally {
      setIsRegistering(false);
    }
  };

  const confirmUnregister = async () => {
    setShowUnregisterDialog(false);
    setIsUnregistering(true);

    try {
      const response = await fetch(func2url.tournaments, {
        method: 'DELETE',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          tournament_id: Number(id),
          steam_id: user.steamId
        })
      });

      const data = await response.json();

      if (response.ok) {
        toast({
          title: "Успешно",
          description: "Регистрация отменена"
        });
        await loadTournamentDetails();
      } else {
        toast({
          title: "Ошибка",
          description: data.error || 'Ошибка отмены регистрации',
          variant: "destructive"
        });
      }
    } catch (error) {
      console.error('Unregistration failed:', error);
      toast({
        title: "Ошибка",
        description: "Ошибка при отмене регистрации",
        variant: "destructive"
      });
    } finally {
      setIsUnregistering(false);
    }
  };

  const handleConfirmParticipation = async () => {
    if (!user || !tournament) return;

    setIsConfirming(true);

    try {
      const response = await fetch(func2url.tournaments, {
        method: 'PATCH',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          tournament_id: tournament.id,
          steam_id: user.steamId
        })
      });

      const data = await response.json();

      if (response.ok) {
        toast({
          title: "Успешно!",
          description: "Участие подтверждено!"
        });
        await loadTournamentDetails();
      } else {
        toast({
          title: "Ошибка",
          description: data.error || 'Ошибка подтверждения',
          variant: "destructive"
        });
      }
    } catch (error) {
      console.error('Confirmation failed:', error);
      toast({
        title: "Ошибка",
        description: "Ошибка при подтверждении участия",
        variant: "destructive"
      });
    } finally {
      setIsConfirming(false);
    }
  };

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

  return (
    <main className="container mx-auto px-6 py-16">
      <Button 
        variant="ghost" 
        onClick={() => navigate('/tournaments')}
        className="mb-6 gap-2"
      >
        <Icon name="ArrowLeft" size={16} />
        Назад к турнирам
      </Button>

      <div className="space-y-8">
        <TournamentInfo tournament={tournament} />

        {tournament.status === 'upcoming' && getTimeUntilStart(tournament.start_date) && (
          <CountdownTimer startDate={tournament.start_date} />
        )}

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
          onConfirm={handleConfirmParticipation}
        />

        <div className="space-y-4">
          <div className="flex gap-1 p-1 bg-muted/40 rounded-xl w-fit border border-border flex-wrap">
            {([
              { key: 'rules', icon: 'BookOpen', label: 'Правила' },
              { key: 'participants', icon: 'Users', label: `Участники (${tournament.participants.length})` },
              { key: 'bracket', icon: 'GitBranch', label: 'Сетка' },
              { key: 'prizes', icon: 'Trophy', label: 'Призы' },
            ] as const).map(tab => (
              <button
                key={tab.key}
                onClick={() => setActiveTab(tab.key)}
                className={`flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium transition-all duration-200 ${
                  activeTab === tab.key
                    ? 'bg-background text-foreground shadow-sm border border-border'
                    : 'text-muted-foreground hover:text-foreground'
                }`}
              >
                <Icon name={tab.icon} size={15} />
                {tab.label}
              </button>
            ))}
          </div>

          <div className="relative">
            {activeTab === 'rules' && (
              <div className="animate-in fade-in duration-200">
                {tournament.rules ? (
                  <div className="prose prose-invert max-w-none whitespace-pre-wrap text-sm text-foreground leading-relaxed p-6 rounded-xl border border-border bg-card/50">
                    {tournament.rules}
                  </div>
                ) : (
                  <div className="p-10 text-center text-muted-foreground border border-dashed border-border rounded-xl">
                    <Icon name="BookOpen" size={36} className="mx-auto mb-3 opacity-30" />
                    <p>Правила турнира не указаны</p>
                  </div>
                )}
              </div>
            )}
            {activeTab === 'participants' && (
              <div className="animate-in fade-in duration-200">
                <ParticipantsList participants={tournament.participants} />
              </div>
            )}
            {activeTab === 'bracket' && (
              <div className="animate-in fade-in duration-200">
                <BracketView
                  participants={tournament.participants}
                  maxParticipants={tournament.max_participants}
                  status={tournament.status}
                  bracketType={tournament.bracket_type || 'random'}
                  tournamentId={tournament.id}
                  onMatchClick={(tId, rIdx, mIdx, players) => {
                    const p1 = players[0]?.steam_id ?? '';
                    const p2 = players[1]?.steam_id ?? '';
                    navigate(`/tournament/${tId}/match/${rIdx}/${mIdx}?p1=${p1}&p2=${p2}`);
                  }}
                />
              </div>
            )}
            {activeTab === 'prizes' && (
              <div className="animate-in fade-in duration-200">
                {tournament.prizes_description ? (
                  <div className="prose prose-invert max-w-none whitespace-pre-wrap text-sm text-foreground leading-relaxed p-6 rounded-xl border border-border bg-card/50">
                    {tournament.prizes_description}
                  </div>
                ) : (
                  <div className="p-10 text-center text-muted-foreground border border-dashed border-border rounded-xl">
                    <Icon name="Trophy" size={36} className="mx-auto mb-3 opacity-30" />
                    <p>Призовая информация не указана</p>
                  </div>
                )}
              </div>
            )}
          </div>
        </div>
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
            <AlertDialogAction onClick={confirmUnregister} className="bg-destructive hover:bg-destructive/90">
              Отменить регистрацию
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </main>
  );
};

export default TournamentDetail;