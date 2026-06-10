import { useState, useEffect } from 'react';
import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import Icon from '@/components/ui/icon';
import func2url from '../../backend/func2url.json';

interface Product {
  id: number;
  name: string;
  amount: string;
  price: number;
  is_active?: boolean;
  category?: string;
}

interface SteamUser {
  steamId: string;
  personaName: string;
  avatarUrl: string;
  profileUrl: string;
}

interface UserBalance {
  balance: number;
}

const Shop = () => {
  const [products, setProducts] = useState<Product[]>([]);
  const [isLoginOpen, setIsLoginOpen] = useState(false);
  const [isRegisterOpen, setIsRegisterOpen] = useState(false);
  const [user, setUser] = useState<SteamUser | null>(null);
  const [balance, setBalance] = useState<number | null>(null);

  useEffect(() => {
    const savedUser = localStorage.getItem('steamUser');
    if (savedUser) {
      const userData = JSON.parse(savedUser);
      setUser(userData);
      loadBalance(userData.steamId);
    }

    loadProducts();

    const params = new URLSearchParams(window.location.search);
    const claimedId = params.get('openid.claimed_id');
    
    if (claimedId) {
      const verifyParams = new URLSearchParams();
      params.forEach((value, key) => {
        verifyParams.append(key, value);
      });
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
    if (cachedProducts) {
      setProducts(JSON.parse(cachedProducts));
    }

    try {
      const timestamp = new Date().getTime();
      const response = await fetch(`${func2url['shop-items']}?_=${timestamp}`);
      
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      
      const data = await response.json();
      setProducts(data.items || []);
      localStorage.setItem('shopProducts', JSON.stringify(data.items || []));
    } catch (error) {
      console.error('Failed to load shop items:', error);
    }
  };

  const loadBalance = async (steamId: string) => {
    const cacheKey = `balance_${steamId}`;
    const cachedBalance = localStorage.getItem(cacheKey);
    if (cachedBalance) {
      setBalance(JSON.parse(cachedBalance));
    }

    try {
      const response = await fetch(`${func2url['balance']}?steam_id=${steamId}`);
      const data = await response.json();
      setBalance(data.balance);
      localStorage.setItem(cacheKey, JSON.stringify(data.balance));
    } catch (error) {
      console.error('Failed to load balance:', error);
    }
  };

  const handleSteamLogin = async () => {
    const returnUrl = `${window.location.origin}${window.location.pathname}`;
    const response = await fetch(`https://functions.poehali.dev/1fc223ef-7704-4b55-a8b5-fea6b000272f?mode=login&return_url=${encodeURIComponent(returnUrl)}`);
    const data = await response.json();
    
    if (data.redirectUrl) {
      window.location.href = data.redirectUrl;
    }
  };

  const handleLogout = () => {
    setUser(null);
    localStorage.removeItem('steamUser');
  };

  return (
      <main className="container mx-auto px-6 py-16">
        <div className="space-y-10">
          <div className="space-y-3">
            <div className="inline-block px-4 py-1.5 bg-primary/10 border border-primary/20 rounded-full mb-2">
              <span className="text-sm font-medium text-primary">Пополнение баланса</span>
            </div>
            <p className="text-muted-foreground text-xl">Пополняйте баланс</p>
          </div>
          
          {user && balance !== null && (
            <Card className="p-6 backdrop-blur-sm bg-card/50 border-border">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-4">
                  <div className="w-16 h-16 rounded-xl bg-gradient-to-br from-primary/20 to-primary/5 flex items-center justify-center">
                    <Icon name="Wallet" size={32} className="text-primary" />
                  </div>
                  <div>
                    <p className="text-sm text-muted-foreground">Ваш баланс</p>
                    <p className="text-3xl font-bold">{balance} ₽</p>
                  </div>
                </div>
                <Button size="lg" className="h-12 px-6">
                  <Icon name="Plus" size={20} className="mr-2" />
                  Пополнить
                </Button>
              </div>
            </Card>
          )}
          
          <div className="space-y-8">
            {(() => {
              const groups: { category: string; items: Product[] }[] = [];
              products.forEach(product => {
                const cat = product.category?.trim() || '';
                const existing = groups.find(g => g.category === cat);
                if (existing) existing.items.push(product);
                else groups.push({ category: cat, items: [product] });
              });
              return groups.map(({ category, items }) => (
                <div key={category}>
                  {category && (
                    <h2 className="text-lg font-bold mb-3 flex items-center gap-2">
                      <span className="w-1 h-5 rounded-full bg-primary inline-block" />
                      {category}
                    </h2>
                  )}
                  <div className="grid gap-2">
                    {items.map(product => (
                      <Card key={product.id} className="group px-4 py-3 backdrop-blur-sm bg-card/50 border-border hover:border-primary/50 transition-all duration-200">
                        <div className="flex items-center gap-4">
                          <div className="w-9 h-9 rounded-lg bg-primary/10 flex items-center justify-center flex-shrink-0 group-hover:bg-primary/20 transition-colors">
                            <Icon name="Coins" size={18} className="text-primary" />
                          </div>
                          <div className="flex-1 min-w-0">
                            <p className="font-semibold text-sm leading-tight truncate">{product.name}</p>
                            <p className="text-xs text-muted-foreground truncate">{product.amount}</p>
                          </div>
                          <div className="flex items-center gap-3 flex-shrink-0">
                            <span className="text-xl font-bold">{product.price}<span className="text-sm text-muted-foreground ml-0.5">₽</span></span>
                            <Button size="sm" className="h-8 px-3 text-xs">
                              <Icon name="ShoppingCart" size={14} className="mr-1" />
                              Купить
                            </Button>
                          </div>
                        </div>
                      </Card>
                    ))}
                  </div>
                </div>
              ));
            })()}
          </div>
        </div>
      </main>
  );
};

export default Shop;