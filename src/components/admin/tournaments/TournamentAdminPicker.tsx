import { useState, useEffect, useRef } from 'react';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import Icon from '@/components/ui/icon';
import func2url from '../../../../backend/func2url.json';

const USERS_URL: string = (func2url as Record<string, string>)['users'] ?? '';

interface UserOption {
  steamId: string;
  personaName: string;
  avatarUrl: string | null;
}

interface TournamentAdminPickerProps {
  value: string[];
  onChange: (ids: string[]) => void;
  adminSteamId: string;
}

export default function TournamentAdminPicker({ value, onChange, adminSteamId }: TournamentAdminPickerProps) {
  const [query, setQuery] = useState('');
  const [allUsers, setAllUsers] = useState<UserOption[]>([]);
  const [isOpen, setIsOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!USERS_URL || !adminSteamId) return;
    fetch(USERS_URL, {
      headers: { 'X-Admin-Steam-Id': adminSteamId }
    })
      .then(r => r.json())
      .then(d => {
        const users: UserOption[] = (d.users || []).map((u: { steam_id: string; persona_name: string | null; avatar_url: string | null }) => ({
          steamId: u.steam_id ?? '',
          personaName: u.persona_name ?? u.steam_id ?? '',
          avatarUrl: u.avatar_url,
        }));
        setAllUsers(users);
      })
      .catch(() => {});
  }, [adminSteamId]);

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setIsOpen(false);
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, []);

  const safeValue = Array.isArray(value) ? value : [];

  const filtered = allUsers.filter(u =>
    !safeValue.includes(u.steamId) &&
    ((u.personaName ?? '').toLowerCase().includes(query.toLowerCase()) || (u.steamId ?? '').includes(query))
  );

  const selected = allUsers.filter(u => safeValue.includes(u.steamId));

  const add = (u: UserOption) => {
    onChange([...safeValue, u.steamId]);
    setQuery('');
    setIsOpen(false);
  };

  const remove = (steamId: string) => {
    onChange(safeValue.filter(id => id !== steamId));
  };

  return (
    <div>
      <Label>Администраторы турнира</Label>

      {/* Выбранные */}
      {selected.length > 0 && (
        <div className="flex flex-wrap gap-2 mt-2 mb-2">
          {selected.map(u => (
            <div key={u.steamId} className="flex items-center gap-1.5 px-2 py-1 rounded-lg bg-primary/10 border border-primary/30 text-sm">
              {u.avatarUrl && <img src={u.avatarUrl} className="w-5 h-5 rounded-full" />}
              <span className="font-medium">{u.personaName}</span>
              <button
                type="button"
                onClick={() => remove(u.steamId)}
                className="ml-0.5 text-muted-foreground hover:text-foreground transition-colors"
              >
                <Icon name="X" size={13} />
              </button>
            </div>
          ))}
        </div>
      )}

      {/* Поиск */}
      <div ref={ref} className="relative mt-1">
        <Input
          value={query}
          onChange={e => { setQuery(e.target.value); setIsOpen(true); }}
          onFocus={() => setIsOpen(true)}
          placeholder="Найти пользователя..."
          className="h-9 text-sm"
        />
        {isOpen && filtered.length > 0 && (
          <div className="absolute z-50 w-full mt-1 bg-card border border-border rounded-lg shadow-xl max-h-48 overflow-y-auto">
            {filtered.slice(0, 10).map(u => (
              <button
                key={u.steamId}
                type="button"
                className="w-full flex items-center gap-2.5 px-3 py-2 text-sm hover:bg-muted/60 transition-colors text-left"
                onClick={() => add(u)}
              >
                {u.avatarUrl
                  ? <img src={u.avatarUrl} className="w-7 h-7 rounded-full flex-shrink-0" />
                  : <div className="w-7 h-7 rounded-full bg-primary/20 flex-shrink-0" />
                }
                <div>
                  <p className="font-medium leading-tight">{u.personaName}</p>
                  <p className="text-xs text-muted-foreground">{u.steamId}</p>
                </div>
              </button>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}