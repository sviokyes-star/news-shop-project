import { useState, useEffect } from 'react';
import ShopTab from '@/components/ShopTab';
import func2url from '../../backend/func2url.json';

interface Product {
  id: number;
  name: string;
  amount: string;
  price: number;
  is_active?: boolean;
  category?: string;
  is_slider: boolean;
  slider_min: number;
  slider_max: number;
  slider_step: number;
  unit_price: number;
  unit_name: string;
}

interface SteamUser {
  steamId: string;
  personaName: string;
  avatarUrl: string;
  profileUrl: string;
}

const Shop = () => {
  const [products, setProducts] = useState<Product[]>([]);
  const [user, setUser] = useState<SteamUser | null>(null);

  useEffect(() => {
    const savedUser = localStorage.getItem('steamUser');
    if (savedUser) setUser(JSON.parse(savedUser));

    loadProducts();

    const params = new URLSearchParams(window.location.search);
    const claimedId = params.get('openid.claimed_id');
    if (claimedId) {
      const verifyParams = new URLSearchParams();
      params.forEach((value, key) => verifyParams.append(key, value));
      verifyParams.append('mode', 'verify');
      fetch(`https://functions.poehali.dev/1fc223ef-7704-4b55-a8b5-fea6b000272f?${verifyParams.toString()}`)
        .then(res => res.json())
        .then(data => {
          if (data.steamId) {
            setUser(data);
            localStorage.setItem('steamUser', JSON.stringify(data));
            window.history.replaceState({}, '', window.location.pathname);
          }
        })
        .catch(err => console.error('Steam auth error:', err));
    }
  }, []);

  const loadProducts = async () => {
    const cachedProducts = localStorage.getItem('shopProducts');
    if (cachedProducts) setProducts(JSON.parse(cachedProducts));

    try {
      const timestamp = new Date().getTime();
      const response = await fetch(`${func2url['shop-items']}?_=${timestamp}`);
      if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
      const data = await response.json();
      setProducts(data.items || []);
      localStorage.setItem('shopProducts', JSON.stringify(data.items || []));
    } catch (error) {
      console.error('Failed to load shop items:', error);
    }
  };

  return (
    <main className="container mx-auto px-6 py-16">
      <ShopTab products={products} user={user} />
    </main>
  );
};

export default Shop;
