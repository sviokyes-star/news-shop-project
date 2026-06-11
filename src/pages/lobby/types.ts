export interface SteamUser {
  steamId: string;
  personaName: string;
  avatarUrl: string;
  isAdmin?: boolean;
}

export interface Player {
  steam_id: string;
  persona_name: string;
  avatar_url: string | null;
}

export interface Message {
  id: number;
  steam_id: string;
  persona_name: string;
  avatar_url: string | null;
  message: string;
  image_url: string | null;
  created_at: string;
}

export interface Lobby {
  id: number;
  player1_steam_id: string | null;
  player2_steam_id: string | null;
  winner_steam_id: string | null;
  status: string;
  player1_reported_winner: string | null;
  player2_reported_winner: string | null;
  is_dispute: boolean;
  admin_steam_id: string | null;
  player1_ready: boolean;
  player2_ready: boolean;
  ready_deadline: string | null;
}

export interface LobbyData {
  lobby: Lobby;
  messages: Message[];
  player1: Player | null;
  player2: Player | null;
}

export interface TournamentAdmin {
  steam_id: string;
  persona_name: string;
  avatar_url: string | null;
}

export const STATUS_LABELS: Record<string, { label: string; color: string }> = {
  waiting:   { label: 'Ожидание',  color: 'text-yellow-500' },
  active:    { label: 'Идёт',      color: 'text-green-500' },
  dispute:   { label: 'Спор',      color: 'text-orange-500' },
  completed: { label: 'Завершён',  color: 'text-muted-foreground' },
};