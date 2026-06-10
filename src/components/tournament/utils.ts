import { Participant } from './types';

export const getTimeUntilStart = (dateString: string) => {
  const start = new Date(dateString).getTime();
  const now = Date.now();
  const diff = start - now;

  if (diff <= 0) return null;

  const days = Math.floor(diff / (1000 * 60 * 60 * 24));
  const hours = Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
  const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
  const seconds = Math.floor((diff % (1000 * 60)) / 1000);

  return { days, hours, minutes, seconds };
};

export const getConfirmationTimeLeft = (dateString: string) => {
  const start = new Date(dateString).getTime();
  const now = Date.now();
  const oneHourBefore = start - (60 * 60 * 1000);
  const diff = start - now;

  if (diff <= 0 || now < oneHourBefore) return null;

  const minutes = Math.floor(diff / (1000 * 60));
  const seconds = Math.floor((diff % (1000 * 60)) / 1000);

  return { minutes, seconds };
};

export const isConfirmationActive = (dateString: string) => {
  const start = new Date(dateString).getTime();
  const now = Date.now();
  const oneHourBefore = start - (60 * 60 * 1000);
  
  return now >= oneHourBefore && now < start;
};

export const isRegistrationClosed = (dateString: string) => {
  const start = new Date(dateString).getTime();
  const now = Date.now();
  
  return now >= start;
};

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

export const findUserMatch = (
  participants: Participant[],
  bracketType: string,
  steamId: string,
  matchLobbies: { round_index: number; match_index: number; player1_steam_id: string | null; player2_steam_id: string | null; winner_steam_id: string | null; status: string }[] = []
): { roundIndex: number; matchIndex: number; players: (Participant | null)[] } | null => {
  const participantMap = new Map(participants.map(p => [p.steam_id, p]));

  // Ищем в реальных лобби — берём матч где игрок участвует и нет победителя (ещё не завершён)
  const activeLobby = matchLobbies
    .filter(ml =>
      (ml.player1_steam_id === steamId || ml.player2_steam_id === steamId) &&
      ml.status !== 'completed'
    )
    .sort((a, b) => b.round_index - a.round_index)[0]; // самый поздний раунд

  if (activeLobby) {
    const p1 = activeLobby.player1_steam_id
      ? (participantMap.get(activeLobby.player1_steam_id) ?? { steam_id: activeLobby.player1_steam_id, persona_name: activeLobby.player1_steam_id.slice(-6), avatar_url: null, registered_at: '', confirmed_at: '' } as Participant)
      : null;
    const p2 = activeLobby.player2_steam_id
      ? (participantMap.get(activeLobby.player2_steam_id) ?? { steam_id: activeLobby.player2_steam_id, persona_name: activeLobby.player2_steam_id.slice(-6), avatar_url: null, registered_at: '', confirmed_at: '' } as Participant)
      : null;
    return { roundIndex: activeLobby.round_index, matchIndex: activeLobby.match_index, players: [p1, p2] };
  }

  // Фолбэк — вычисляем из первого раунда (если лобби ещё не созданы)
  const pool = participants.filter(p => p.confirmed_at).length >= 2
    ? participants.filter(p => p.confirmed_at)
    : participants;

  let sorted: Participant[];
  if (bracketType === 'rating') {
    const byRating = [...pool].sort((a, b) => (b.rating ?? 0) - (a.rating ?? 0));
    const result: Participant[] = [];
    let lo = 0, hi = byRating.length - 1;
    while (lo <= hi) {
      if (lo === hi) { result.push(byRating[lo++]); break; }
      result.push(byRating[lo++]);
      result.push(byRating[hi--]);
    }
    sorted = result;
  } else {
    const seed = pool.reduce((acc, p) => acc + parseInt(p.steam_id.slice(-4), 16), 0);
    sorted = seededShuffle(pool, seed);
  }

  const size = Math.pow(2, Math.ceil(Math.log2(Math.max(sorted.length, 2))));
  const slots: (Participant | null)[] = [...sorted];
  while (slots.length < size) slots.push(null);

  const firstRound: (Participant | null)[][] = [];
  for (let i = 0; i < slots.length; i += 2) firstRound.push([slots[i], slots[i + 1]]);

  for (let mIdx = 0; mIdx < firstRound.length; mIdx++) {
    const pair = firstRound[mIdx];
    if (pair.some(p => p?.steam_id === steamId)) {
      return { roundIndex: 0, matchIndex: mIdx, players: pair };
    }
  }
  return null;
};

export const getTimeUntilConfirmation = (dateString: string) => {
  const start = new Date(dateString).getTime();
  const now = Date.now();
  const oneHourBefore = start - (60 * 60 * 1000);
  const diff = oneHourBefore - now;

  if (diff <= 0 || now >= start) return null;

  const days = Math.floor(diff / (1000 * 60 * 60 * 24));
  const hours = Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
  const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
  const seconds = Math.floor((diff % (1000 * 60)) / 1000);

  return { days, hours, minutes, seconds };
};