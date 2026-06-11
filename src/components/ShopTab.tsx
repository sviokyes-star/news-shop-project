import { useState, useEffect } from 'react';
import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Slider } from '@/components/ui/slider';
import Icon from '@/components/ui/icon';
import func2url from '../../backend/func2url.json';

interface Product {
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
  unit_multiplier: number;
  category?: string;
}

interface SteamUser {
  steamId: string;
  personaName: string;
  avatarUrl: string;
  profileUrl: string;
}

interface ShopTabProps {
  products: Product[];
  user: SteamUser | null;
}

const ShopTab = ({ products, user }: ShopTabProps) => {
  const [balance, setBalance] = useState<number>(0);
  const [isLoadingBalance, setIsLoadingBalance] = useState(false);
  const [purchasingItemId, setPurchasingItemId] = useState<number | null>(null);
  const [isCreatingPayment, setIsCreatingPayment] = useState(false);
  const [isTopUpDialogOpen, setIsTopUpDialogOpen] = useState(false);
  const [customAmount, setCustomAmount] = useState<string>('');
  const [sliderValues, setSliderValues] = useState<Record<number, number>>({});

  useEffect(() => {
    if (user) loadBalance();
  }, [user]);

  useEffect(() => {
    const initial: Record<number, number> = {};
    products.forEach(p => {
      if (p.is_slider) initial[p.id] = p.slider_min;
    });
    setSliderValues(initial);
  }, [products]);

  const loadBalance = async () => {
    if (!user) return;
    setIsLoadingBalance(true);
    try {
      const response = await fetch(`${func2url.balance}?steam_id=${user.steamId}`);
      const data = await response.json();
      setBalance(data.balance || 0);
    } catch (error) {
      console.error('Failed to load balance:', error);
    } finally {
      setIsLoadingBalance(false);
    }
  };

  const handleTopUp = async (productId?: number) => {
    if (!user) {
      alert('Войдите через Steam для пополнения баланса');
      return;
    }

    let selectedProduct: Product | undefined;
    if (productId) {
      selectedProduct = products.find(p => p.id === productId);
    } else if (customAmount) {
      const amount = parseFloat(customAmount);
      if (isNaN(amount) || amount < 10) {
        alert('Минимальная сумма пополнения: 10 ₽');
        return;
      }
      selectedProduct = products.find(p => p.price === amount) || products[0];
    } else {
      selectedProduct = products[0];
    }

    if (!selectedProduct) {
      alert('Нет доступных товаров для пополнения');
      return;
    }

    setIsCreatingPayment(true);
    try {
      const response = await fetch(func2url.payment, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          steam_id: user.steamId,
          persona_name: user.personaName,
          shop_item_id: selectedProduct.id
        })
      });

      const data = await response.json();
      if (response.ok && data.payment_url) {
        window.open(data.payment_url, '_blank');
        setIsTopUpDialogOpen(false);
        setCustomAmount('');
      } else {
        alert(data.error || 'Ошибка при создании платежа');
      }
    } catch (error) {
      console.error('Payment creation failed:', error);
      alert('Ошибка при создании платежа');
    } finally {
      setIsCreatingPayment(false);
    }
  };

  const handleBuy = async (product: Product, quantity?: number) => {
    if (!user) {
      alert('Войдите через Steam для покупки');
      return;
    }

    const totalPrice = product.is_slider && quantity
      ? quantity * product.unit_price
      : product.price;

    if (balance < totalPrice) {
      alert(`Недостаточно средств! Требуется ${totalPrice} ₽, у вас ${balance} ₽`);
      return;
    }

    setPurchasingItemId(product.id);
    try {
      const response = await fetch(func2url.purchases, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          steam_id: user.steamId,
          persona_name: user.personaName,
          shop_item_id: product.id,
          ...(product.is_slider && quantity ? { quantity } : {})
        })
      });

      const data = await response.json();
      if (response.ok && data.success) {
        setBalance(data.new_balance);
        alert(`Успешно куплено: ${data.item_name}${quantity ? ` × ${quantity}` : ''}`);
      } else {
        alert(data.error || 'Ошибка при покупке');
      }
    } catch (error) {
      console.error('Purchase failed:', error);
      alert('Ошибка при покупке');
    } finally {
      setPurchasingItemId(null);
    }
  };

  return (
    <div className="space-y-10">
      <div className="space-y-3">
        <div className="inline-block px-4 py-1.5 bg-primary/10 border border-primary/20 rounded-full mb-2">
          <span className="text-sm font-medium text-primary">Пополнение</span>
        </div>
        <p className="text-muted-foreground text-xl">Пополните баланс рублей</p>
      </div>

      {user && (
        <Card className="p-6 bg-gradient-to-r from-primary/10 to-primary/5 border-primary/20">
          <div className="flex items-center justify-between">
            <div className="space-y-1">
              <p className="text-sm text-muted-foreground">Ваш баланс</p>
              <div className="flex items-center gap-3">
                <Icon name="Wallet" size={32} className="text-primary" />
                {isLoadingBalance ? (
                  <span className="text-3xl font-bold">...</span>
                ) : (
                  <span className="text-4xl font-bold">{balance.toLocaleString('ru-RU', { timeZone: 'Europe/Moscow' })}</span>
                )}
                <span className="text-2xl text-muted-foreground">₽</span>
              </div>
            </div>
            <div className="flex items-center gap-2">
              <Dialog open={isTopUpDialogOpen} onOpenChange={setIsTopUpDialogOpen}>
                <DialogTrigger asChild>
                  <Button size="lg" className="gap-2" disabled={isCreatingPayment}>
                    <Icon name="Plus" size={18} />
                    Пополнить
                  </Button>
                </DialogTrigger>
                <DialogContent className="sm:max-w-md">
                  <DialogHeader>
                    <DialogTitle>Пополнение баланса</DialogTitle>
                    <DialogDescription>Укажите сумму</DialogDescription>
                  </DialogHeader>
                  <div className="space-y-4">
                    <div className="space-y-2">
                      <Label htmlFor="custom-amount">Сумма (₽)</Label>
                      <Input
                        id="custom-amount"
                        type="number"
                        placeholder="Минимум 10 ₽"
                        value={customAmount}
                        onChange={(e) => setCustomAmount(e.target.value)}
                        min="10"
                      />
                    </div>
                    <Button
                      onClick={() => handleTopUp()}
                      disabled={isCreatingPayment || !customAmount}
                      className="w-full"
                    >
                      {isCreatingPayment ? (
                        <><Icon name="Loader2" size={18} className="animate-spin mr-2" />Создание...</>
                      ) : (
                        <>Пополнить {customAmount ? `${customAmount} ₽` : ''}</>
                      )}
                    </Button>
                  </div>
                </DialogContent>
              </Dialog>
              <Button variant="outline" size="sm" onClick={loadBalance} disabled={isLoadingBalance}>
                <Icon name="RefreshCw" size={16} className={isLoadingBalance ? 'animate-spin' : ''} />
              </Button>
            </div>
          </div>
        </Card>
      )}

      <div id="topup-products" className="space-y-8">
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
                {items.map(product => {
                  const multiplier = product.unit_multiplier ?? 1;
                  const qty = sliderValues[product.id] ?? product.slider_min;
                  const totalPrice = product.is_slider ? qty * product.unit_price : product.price;
                  const totalUnits = product.is_slider ? qty * multiplier : 0;
                  return (
                    <Card
                      key={product.id}
                      className="group px-4 py-3 border-border hover:border-primary/50 transition-all duration-200 bg-card/50 backdrop-blur"
                    >
                      <div className="flex items-center gap-4">
                        <div className="w-9 h-9 rounded-lg bg-primary/10 flex items-center justify-center flex-shrink-0 group-hover:bg-primary/20 transition-colors">
                          <Icon name="Coins" size={18} className="text-primary" />
                        </div>

                        <div className="flex-1 min-w-0">
                          <p className="font-semibold text-sm leading-tight truncate">{product.name}</p>
                          {product.amount && <p className="text-xs text-muted-foreground truncate">{product.amount}</p>}

                          {product.is_slider && (
                            <div className="flex items-center gap-3 mt-2">
                              <Slider
                                min={product.slider_min}
                                max={product.slider_max}
                                step={product.slider_step}
                                value={[qty]}
                                onValueChange={([val]) => setSliderValues(prev => ({ ...prev, [product.id]: val }))}
                                className="w-32"
                              />
                              <input
                                type="number"
                                min={product.slider_min}
                                max={product.slider_max}
                                step={product.slider_step}
                                value={multiplier > 1 ? totalUnits : qty}
                                onChange={(e) => {
                                  let raw = Number(e.target.value);
                                  if (multiplier > 1) raw = Math.round(raw / multiplier);
                                  const clamped = Math.max(product.slider_min, Math.min(product.slider_max, raw));
                                  setSliderValues(prev => ({ ...prev, [product.id]: clamped }));
                                }}
                                className="w-20 text-xs font-semibold bg-background border border-border rounded px-2 py-1 text-center focus:outline-none focus:border-primary"
                              />
                              <span className="text-xs text-muted-foreground whitespace-nowrap">{product.unit_name}</span>
                            </div>
                          )}
                        </div>

                        <div className="flex items-center gap-3 flex-shrink-0">
                          <div className="text-right">
                            <span className="text-xl font-bold">{totalPrice}</span>
                            <span className="text-sm text-muted-foreground ml-0.5">₽</span>
                            {product.is_slider && (
                              <p className="text-xs text-muted-foreground">
                                {multiplier > 1 ? `${multiplier} ${product.unit_name} = ${product.unit_price} ₽` : `1 ${product.unit_name} = ${product.unit_price} ₽`}
                              </p>
                            )}
                          </div>
                          <Button
                            size="sm"
                            className="h-8 px-3 text-xs"
                            onClick={() => handleBuy(product, product.is_slider ? qty : undefined)}
                            disabled={!user || purchasingItemId === product.id}
                          >
                            {purchasingItemId === product.id ? (
                              <Icon name="Loader2" size={14} className="animate-spin" />
                            ) : (
                              <><Icon name="ShoppingCart" size={14} className="mr-1" />Купить</>
                            )}
                          </Button>
                        </div>
                      </div>
                    </Card>
                  );
                })}
              </div>
            </div>
          ));
        })()}
      </div>
    </div>
  );
};

export default ShopTab;