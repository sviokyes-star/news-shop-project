export interface Participant {
  steam_id: string;
  persona_name: string;
  avatar_url: string;
  registered_at: string;
  confirmed_at?: string | null;
  is_admin?: boolean;
  is_moderator?: boolean;
  is_online?: boolean;
  last_online?: string | null;
}

export interface TournamentDetail {
  id: number;
  name: string;
  description: string;
  prize_pool: number;
  max_participants: number;
  status: string;
  tournament_type: string;
  game: string;
  start_date: string;
  participants_count: number;
  participants: Participant[];
  confirmed_at?: string | null;
}

export interface SteamUser {
  steamId: string;
  personaName: string;
  avatarUrl: string;
  profileUrl: string;
}