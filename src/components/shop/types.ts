export interface Transaction {
  id: number;
  amount: number;
  type: string;
  description: string;
  created_at: string;
}

export { formatShortDateTime as formatDate } from '@/utils/dateFormat';

export interface Product {
  id: number;
  name: string;
  amount: string;
  price: number;
  is_slider: boolean;
  slider_min: number;
  slider_max: number;
  slider_step: number;
  unit_price: number;
  unit_name: string;
  unit_multiplier?: number;
  category?: string;
}

export interface SteamUser {
  steamId: string;
  personaName: string;
  avatarUrl: string;
  profileUrl: string;
}