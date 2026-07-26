import { useState, useEffect } from 'react';
import func2url from '../../backend/func2url.json';
import { toast } from '@/hooks/use-toast';
import { Transaction, Product, SteamUser } from './shop/types';
import BalanceCard from './shop/BalanceCard';
import ProductList from './shop/ProductList';

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
  const [isHistoryOpen, setIsHistoryOpen] = useState(false);
  const [history, setHistory] = useState<Transaction[]>([]);
  const [isLoadingHistory, setIsLoadingHistory] = useState(false);
  const [delivering, setDelivering] = useState<{ purchaseId: number; productId: number }[]>([]);

  useEffect(() => {
    if (user) loadBalance();
  }, [user]);

  useEffect(() => {
    if (!user || delivering.length === 0) return;

    const checkDelivery = async () => {
      try {
        const response = await fetch(`${func2url['game-sync']}?action=status&steam_id=${user.steamId}`);
        if (!response.ok) return;
        const data = await response.json();
        const pendingPurchaseIds: number[] = (data.pending || []).map((p: { purchase_id: number }) => p.purchase_id);
        setDelivering(prev => {
          const stillPending = prev.filter(d => pendingPurchaseIds.includes(d.purchaseId));
          const delivered = prev.filter(d => !pendingPurchaseIds.includes(d.purchaseId));
          if (delivered.length > 0) {
            toast({ title: 'Начислено в игре!', description: 'Загляни на сервер — золото/серебро уже у тебя.' });
          }
          return stillPending;
        });
      } catch (error) {
        console.error('Delivery status check failed:', error);
      }
    };

    checkDelivery();
    const interval = setInterval(checkDelivery, 5000);
    return () => clearInterval(interval);
  }, [user, delivering.length]);

  useEffect(() => {
    const initial: Record<number, number> = {};
    products.forEach(p => {
      if (p.is_slider) initial[p.id] = p.slider_min;
    });
    setSliderValues(initial);
  }, [products]);

  const loadHistory = async () => {
    if (!user) return;
    setIsLoadingHistory(true);
    try {
      const res = await fetch(`${func2url.balance}?steam_id=${user.steamId}&action=history`);
      const data = await res.json();
      setHistory(data.history || []);
    } catch (e) {
      console.error('Failed to load history', e);
    } finally {
      setIsLoadingHistory(false);
    }
  };

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
      toast({ title: 'Войдите через Steam для пополнения баланса', variant: 'destructive' });
      return;
    }

    let selectedProduct: Product | undefined;
    if (productId) {
      selectedProduct = products.find(p => p.id === productId);
    } else if (customAmount) {
      const amount = parseFloat(customAmount);
      if (isNaN(amount) || amount < 10) {
        toast({ title: 'Минимальная сумма пополнения: 10 ₽', variant: 'destructive' });
        return;
      }
      selectedProduct = products.find(p => p.price === amount) || products[0];
    } else {
      selectedProduct = products[0];
    }

    if (!selectedProduct) {
      toast({ title: 'Нет доступных товаров для пополнения', variant: 'destructive' });
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
        toast({ title: 'Ошибка при создании платежа', description: data.error, variant: 'destructive' });
      }
    } catch (error) {
      console.error('Payment creation failed:', error);
      toast({ title: 'Ошибка при создании платежа', variant: 'destructive' });
    } finally {
      setIsCreatingPayment(false);
    }
  };

  const handleBuy = async (product: Product, quantity?: number) => {
    if (!user) {
      toast({ title: 'Войдите через Steam для покупки', variant: 'destructive' });
      return;
    }

    const totalPrice = product.is_slider && quantity
      ? quantity * product.unit_price
      : product.price;

    if (balance < totalPrice) {
      toast({ title: 'Недостаточно средств', description: `Требуется ${totalPrice} ₽, у вас ${balance} ₽`, variant: 'destructive' });
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
        toast({ title: 'Успешно куплено!', description: `${data.item_name}${quantity ? ` × ${quantity}` : ''}` });
        const unit = (product.unit_name || '').toLowerCase();
        const isGameCurrency = unit.includes('золот') || unit.includes('серебр');
        if (isGameCurrency && data.purchase_id) {
          setDelivering(prev => [...prev, { purchaseId: data.purchase_id, productId: product.id }]);
        }
      } else {
        toast({ title: 'Ошибка при покупке', description: data.error || 'Попробуйте ещё раз', variant: 'destructive' });
      }
    } catch (error) {
      console.error('Purchase failed:', error);
      toast({ title: 'Ошибка при покупке', variant: 'destructive' });
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
        <BalanceCard
          balance={balance}
          isLoadingBalance={isLoadingBalance}
          isHistoryOpen={isHistoryOpen}
          setIsHistoryOpen={setIsHistoryOpen}
          loadHistory={loadHistory}
          isLoadingHistory={isLoadingHistory}
          history={history}
          isTopUpDialogOpen={isTopUpDialogOpen}
          setIsTopUpDialogOpen={setIsTopUpDialogOpen}
          isCreatingPayment={isCreatingPayment}
          customAmount={customAmount}
          setCustomAmount={setCustomAmount}
          handleTopUp={handleTopUp}
          loadBalance={loadBalance}
        />
      )}

      <ProductList
        products={products}
        user={user}
        sliderValues={sliderValues}
        setSliderValues={setSliderValues}
        purchasingItemId={purchasingItemId}
        delivering={delivering}
        handleBuy={handleBuy}
      />
    </div>
  );
};

export default ShopTab;
