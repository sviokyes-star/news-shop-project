import { useNavigate } from 'react-router-dom';
import Icon from '@/components/ui/icon';
import ParticipantsList from './ParticipantsList';
import BracketView from './BracketView';
import { TournamentDetail } from './types';

type TabKey = 'rules' | 'participants' | 'bracket' | 'prizes';

interface TournamentTabsProps {
  tournament: TournamentDetail;
  activeTab: TabKey;
  onTabChange: (tab: TabKey) => void;
}

export default function TournamentTabs({ tournament, activeTab, onTabChange }: TournamentTabsProps) {
  const navigate = useNavigate();

  const tabs = [
    { key: 'rules' as const,        icon: 'BookOpen',  label: 'Правила' },
    { key: 'participants' as const,  icon: 'Users',     label: `Участники (${tournament.participants.length})` },
    { key: 'bracket' as const,       icon: 'GitBranch', label: 'Сетка' },
    { key: 'prizes' as const,        icon: 'Trophy',    label: 'Призы' },
  ];

  return (
    <div className="space-y-4">
      <div className="flex gap-1 p-1 bg-muted/40 rounded-xl w-fit border border-border flex-wrap">
        {tabs.map(tab => (
          <button
            key={tab.key}
            onClick={() => onTabChange(tab.key)}
            className={`flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium transition-all duration-200 ${
              activeTab === tab.key
                ? 'bg-background text-foreground shadow-sm border border-border'
                : 'text-muted-foreground hover:text-foreground'
            }`}
          >
            <Icon name={tab.icon as never} size={15} />
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
              matchLobbies={tournament.match_lobbies || []}
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
  );
}
