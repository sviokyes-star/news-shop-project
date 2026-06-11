export interface ShopItem {
  id: number;
  name: string;
  amount: string;
  price: number;
  is_active: boolean;
  order_position: number;
  category: string;
  is_slider: boolean;
  slider_min: number;
  slider_max: number;
  slider_step: number;
  unit_price: number;
  unit_name: string;
  unit_multiplier: number;
}

export interface SteamUser {
  steamId: string;
  personaName: string;
  avatarUrl: string;
  profileUrl: string;
}

export interface ShopFormData {
  name: string;
  amount: string;
  price: number;
  is_active: boolean;
  category: string;
  is_slider: boolean;
  slider_min: number;
  slider_max: number;
  slider_step: number;
  unit_price: number;
  unit_name: string;
  unit_multiplier: number;
}

export const EMPTY_FORM: ShopFormData = {
  name: '',
  amount: '',
  price: 0,
  is_active: true,
  category: '',
  is_slider: false,
  slider_min: 1,
  slider_max: 100,
  slider_step: 1,
  unit_price: 10,
  unit_name: '',
  unit_multiplier: 1
};