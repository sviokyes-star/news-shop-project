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

      <div id="topup-products" className="space-y-6">
        {products.map((product) => {
          const qty = sliderValues[product.id] ?? product.slider_min;
          const totalPrice = product.is_slider ? qty * product.unit_price : product.price;

          return (
            <Card
              key={product.id}
              className="group p-8 border-border hover:border-primary/50 transition-all duration-300 hover:shadow-xl hover:shadow-primary/10 bg-card/50 backdrop-blur"
            >
              <div className="flex items-center justify-between gap-8">
                <div className="flex items-center gap-6 flex-1">
                  <div className="w-20 h-20 rounded-2xl bg-gradient-to-br from-primary/20 to-primary/5 flex items-center justify-center group-hover:scale-110 transition-transform flex-shrink-0">
                    <Icon name="Coins" size={40} className="text-primary" />
                  </div>

                  <div className="space-y-3 flex-1">
                    <h3 className="text-3xl font-bold">{product.name}</h3>
                    <p className="text-xl text-primary font-semibold">{product.amount}</p>

                    {product.is_slider && (
                      <div className="space-y-2 pt-1">
                        <div className="flex items-center justify-between text-sm">
                          <span className="text-muted-foreground">Количество:</span>
                          <span className="font-bold text-lg">{qty} {product.unit_name}</span>
                        </div>
                        <Slider
                          min={product.slider_min}
                          max={product.slider_max}
                          step={product.slider_step}
                          value={[qty]}
                          onValueChange={([val]) => setSliderValues(prev => ({ ...prev, [product.id]: val }))}
                          className="w-full max-w-xs"
                        />
                        <div className="flex justify-between text-xs text-muted-foreground max-w-xs">
                          <span>{product.slider_min}</span>
                          <span>{product.slider_max}</span>
                        </div>
                      </div>
                    )}
                  </div>
                </div>

                <div className="flex items-center gap-8">
                  <div className="text-right">
                    <div className="flex items-baseline gap-1">
                      <span className="text-5xl font-bold">{totalPrice}</span>
                      <span className="text-2xl text-muted-foreground">₽</span>
                    </div>
                    {product.is_slider && (
                      <p className="text-sm text-muted-foreground">{product.unit_price} ₽ / {product.unit_name}</p>
                    )}
                  </div>

                  <Button
                    size="lg"
                    className="h-14 px-8 text-lg font-semibold shadow-lg shadow-primary/20 group-hover:shadow-xl group-hover:shadow-primary/30"
                    onClick={() => handleBuy(product, product.is_slider ? qty : undefined)}
                    disabled={!user || purchasingItemId === product.id}
                  >
                    {purchasingItemId === product.id ? (
                      <><Icon name="Loader2" size={24} className="animate-spin mr-2" />Покупка...</>
                    ) : (
                      <><Icon name="ShoppingCart" size={24} className="mr-2" />Купить</>
                    )}
                  </Button>
                </div>
              </div>
            </Card>
          );
        })}
      </div>
    </div>
  );
};

export default ShopTab;
