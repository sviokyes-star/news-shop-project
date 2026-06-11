import { useState, useEffect } from 'react';
import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import Icon from '@/components/ui/icon';
import func2url from '../../../backend/func2url.json';

interface ShopItem {
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
}

interface SteamUser {
  steamId: string;
  personaName: string;
  avatarUrl: string;
  profileUrl: string;
}

interface ShopManagementProps {
  shopItems: ShopItem[];
  isLoading: boolean;
  onRefresh: () => Promise<void>;
}

export default function ShopManagement({ shopItems, isLoading, onRefresh }: ShopManagementProps) {
  const [user, setUser] = useState<SteamUser | null>(null);

  useEffect(() => {
    const savedUser = localStorage.getItem('steamUser');
    if (savedUser) {
      setUser(JSON.parse(savedUser));
    }
  }, []);
  const [editingShopId, setEditingShopId] = useState<number | null>(null);
  const [error, setError] = useState<string>('');
  const [success, setSuccess] = useState<string>('');
  const [shopFormData, setShopFormData] = useState({
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
    unit_name: ''
  });

  const handleShopSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    console.log('🔵 handleShopSubmit called', { user, editingShopId, shopFormData });
    
    if (!user) {
      console.log('❌ No user');
      setError('Пользователь не авторизован');
      return;
    }
    
    setError('');
    setSuccess('');

    try {
      const url = func2url['shop-items'];
      const method = editingShopId ? 'PUT' : 'POST';
      const body = editingShopId 
        ? { ...shopFormData, id: editingShopId }
        : shopFormData;

      console.log('📤 Sending request:', { url, method, body });

      const response = await fetch(url, {
        method,
        headers: {
          'Content-Type': 'application/json',
          'X-Admin-Steam-Id': user.steamId
        },
        body: JSON.stringify(body)
      });

      console.log('📥 Response:', response.status, response.ok);

      if (response.ok) {
        setSuccess(editingShopId ? 'Товар обновлён' : 'Товар добавлен');
        await onRefresh();
        setTimeout(() => {
          resetShopForm();
        }, 1000);
      } else {
        const data = await response.json();
        console.log('❌ Error response:', data);
        setError(data.error || 'Ошибка при сохранении');
      }
    } catch (error) {
      console.error('❌ Failed to save shop item:', error);
      setError('Ошибка соединения с сервером');
    }
  };

  const handleEditShopItem = (item: ShopItem) => {
    setEditingShopId(item.id);
    setShopFormData({
      name: item.name,
      amount: item.amount,
      price: item.price,
      is_active: item.is_active,
      category: item.category || '',
      is_slider: item.is_slider || false,
      slider_min: item.slider_min ?? 1,
      slider_max: item.slider_max ?? 100,
      slider_step: item.slider_step ?? 1,
      unit_price: item.unit_price ?? 10,
      unit_name: item.unit_name || ''
    });
  };

  const handleDeleteShopItem = async (id: number) => {
    if (!confirm('Удалить этот товар?')) return;
    if (!user) return;

    try {
      const response = await fetch(
        `${func2url['shop-items']}?id=${id}`,
        {
          method: 'DELETE',
          headers: {
            'X-Admin-Steam-Id': user.steamId
          }
        }
      );

      if (response.ok) {
        await onRefresh();
      }
    } catch (error) {
      console.error('Failed to delete shop item:', error);
    }
  };

  const handleToggleActive = async (item: ShopItem) => {
    if (!user) return;

    try {
      const response = await fetch(func2url['shop-items'], {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'X-Admin-Steam-Id': user.steamId
        },
        body: JSON.stringify({
          id: item.id,
          is_active: !item.is_active
        })
      });

      if (response.ok) {
        await onRefresh();
      }
    } catch (error) {
      console.error('Failed to toggle shop item:', error);
    }
  };

  const resetShopForm = () => {
    setEditingShopId(null);
    setShopFormData({
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
      unit_name: ''
    });
    setError('');
    setSuccess('');
  };

  const handleMoveShopItem = async (item: ShopItem, direction: 'up' | 'down') => {
    if (!user) {
      console.log('❌ No user for move');
      return;
    }

    console.log('🔄 Moving item:', item.name, direction);
    
    const sortedItems = [...shopItems].sort((a, b) => a.order_position - b.order_position);
    const currentIndex = sortedItems.findIndex(i => i.id === item.id);
    
    console.log('📊 Current index:', currentIndex, 'Total items:', sortedItems.length);
    
    if (
      (direction === 'up' && currentIndex === 0) ||
      (direction === 'down' && currentIndex === sortedItems.length - 1)
    ) {
      console.log('⛔ Cannot move - at boundary');
      return;
    }

    const swapIndex = direction === 'up' ? currentIndex - 1 : currentIndex + 1;
    const swapItem = sortedItems[swapIndex];

    const originalItemPos = item.order_position;
    const originalSwapPos = swapItem.order_position;

    console.log('🔀 Swapping:', {
      item1: { id: item.id, pos: originalItemPos, newPos: originalSwapPos },
      item2: { id: swapItem.id, pos: originalSwapPos, newPos: originalItemPos }
    });

    try {
      const response1 = await fetch(func2url['shop-items'], {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'X-Admin-Steam-Id': user.steamId
        },
        body: JSON.stringify({
          id: item.id,
          order_position: originalSwapPos
        })
      });

      console.log('✅ Response 1:', response1.status);

      const response2 = await fetch(func2url['shop-items'], {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'X-Admin-Steam-Id': user.steamId
        },
        body: JSON.stringify({
          id: swapItem.id,
          order_position: originalItemPos
        })
      });

      console.log('✅ Response 2:', response2.status);

      await onRefresh();
      console.log('🔄 Reloaded items');
    } catch (error) {
      console.error('❌ Failed to reorder shop items:', error);
    }
  };

  return (
    <div className="grid lg:grid-cols-2 gap-6">
      <div>
        <Card className="p-6 bg-card/80 backdrop-blur border-primary/20">
          <h2 className="text-2xl font-bold mb-6 flex items-center gap-2">
            <Icon name={editingShopId ? "Edit" : "Plus"} size={24} />
            {editingShopId ? 'Редактировать товар' : 'Добавить товар'}
          </h2>
          
          {error && (
            <div className="mb-4 p-3 bg-destructive/10 border border-destructive/20 rounded-lg text-destructive text-sm">
              {error}
            </div>
          )}
          {success && (
            <div className="mb-4 p-3 bg-green-500/10 border border-green-500/20 rounded-lg text-green-500 text-sm">
              {success}
            </div>
          )}
          
          <form onSubmit={handleShopSubmit} className="space-y-4">
            <div>
              <label className="block text-sm font-medium mb-2">Название товара</label>
              <Input
                value={shopFormData.name}
                onChange={(e) => setShopFormData({ ...shopFormData, name: e.target.value })}
                placeholder="VIP статус на месяц"
                required
              />
            </div>

            <div>
              <label className="block text-sm font-medium mb-2">Категория товара</label>
              <Input
                value={shopFormData.category}
                onChange={(e) => setShopFormData({ ...shopFormData, category: e.target.value })}
                placeholder="VIP, Скины, Бонусы..."
              />
            </div>

            <div>
              <label className="block text-sm font-medium mb-2">Количество/Описание</label>
              <Input
                value={shopFormData.amount}
                onChange={(e) => setShopFormData({ ...shopFormData, amount: e.target.value })}
                placeholder="1000 монет, 30 дней, и т.д."
                required
              />
            </div>

            <div>
              <label className="block text-sm font-medium mb-2">Цена (₽)</label>
              <Input
                type="number"
                value={shopFormData.price}
                onChange={(e) => setShopFormData({ ...shopFormData, price: Number(e.target.value) })}
                placeholder="100"
                required
                min="0"
              />
            </div>

            <div className="p-4 border border-border rounded-lg space-y-4">
              <div className="flex items-center gap-2">
                <input
                  type="checkbox"
                  id="is_slider"
                  checked={shopFormData.is_slider}
                  onChange={(e) => setShopFormData({ ...shopFormData, is_slider: e.target.checked })}
                  className="w-4 h-4"
                />
                <label htmlFor="is_slider" className="text-sm font-medium">
                  Товар с ползунком (покупатель выбирает количество)
                </label>
              </div>

              {shopFormData.is_slider && (
                <div className="space-y-3 pl-6 border-l-2 border-primary/30">
                  <div>
                    <label className="block text-sm font-medium mb-1">Название единицы (напр: фишка, монета)</label>
                    <Input
                      value={shopFormData.unit_name}
                      onChange={(e) => setShopFormData({ ...shopFormData, unit_name: e.target.value })}
                      placeholder="фишка"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium mb-1">Цена за 1 единицу (₽)</label>
                    <Input
                      type="number"
                      value={shopFormData.unit_price}
                      onChange={(e) => setShopFormData({ ...shopFormData, unit_price: Number(e.target.value) })}
                      placeholder="10"
                      min="1"
                    />
                  </div>
                  <div className="grid grid-cols-3 gap-3">
                    <div>
                      <label className="block text-sm font-medium mb-1">Мин.</label>
                      <Input
                        type="number"
                        value={shopFormData.slider_min}
                        onChange={(e) => setShopFormData({ ...shopFormData, slider_min: Number(e.target.value) })}
                        min="1"
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-medium mb-1">Макс.</label>
                      <Input
                        type="number"
                        value={shopFormData.slider_max}
                        onChange={(e) => setShopFormData({ ...shopFormData, slider_max: Number(e.target.value) })}
                        min="1"
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-medium mb-1">Шаг</label>
                      <Input
                        type="number"
                        value={shopFormData.slider_step}
                        onChange={(e) => setShopFormData({ ...shopFormData, slider_step: Number(e.target.value) })}
                        min="1"
                      />
                    </div>
                  </div>
                  <p className="text-xs text-muted-foreground">
                    Покупатель выберет от {shopFormData.slider_min} до {shopFormData.slider_max} {shopFormData.unit_name || 'ед.'} с шагом {shopFormData.slider_step}. Итоговая цена = кол-во × {shopFormData.unit_price} ₽
                  </p>
                </div>
              )}
            </div>

            <div className="flex items-center gap-2">
              <input
                type="checkbox"
                id="is_active"
                checked={shopFormData.is_active}
                onChange={(e) => setShopFormData({ ...shopFormData, is_active: e.target.checked })}
                className="w-4 h-4"
              />
              <label htmlFor="is_active" className="text-sm font-medium">
                Активен (отображается в магазине)
              </label>
            </div>

            <div className="flex gap-3">
              <Button type="submit" className="flex-1">
                <Icon name={editingShopId ? "Save" : "Plus"} size={18} className="mr-2" />
                {editingShopId ? 'Сохранить' : 'Добавить'}
              </Button>
              {editingShopId && (
                <Button type="button" variant="outline" onClick={resetShopForm}>
                  Отмена
                </Button>
              )}
            </div>
          </form>
        </Card>
      </div>

      <div>
        <Card className="p-6 bg-card/80 backdrop-blur border-primary/20">
          <h2 className="text-2xl font-bold mb-6 flex items-center gap-2">
            <Icon name="ShoppingBag" size={24} />
            Список товаров ({shopItems.length})
          </h2>
          
          {isLoading ? (
            <div className="text-center py-12 text-muted-foreground">
              Загрузка товаров...
            </div>
          ) : shopItems.length === 0 ? (
            <div className="text-center py-12 text-muted-foreground">
              <Icon name="ShoppingBag" size={48} className="mx-auto mb-3 opacity-20" />
              <p>Товары не добавлены</p>
            </div>
          ) : (
            <div className="space-y-3 max-h-[600px] overflow-y-auto">
              {shopItems
                .sort((a, b) => a.order_position - b.order_position)
                .map((item) => (
                  <div
                    key={item.id}
                    className={`p-4 rounded-lg border bg-background/50 hover:border-primary/30 transition-colors ${
                      item.is_active ? 'border-border' : 'border-muted opacity-60'
                    }`}
                  >
                    <div className="flex items-start justify-between gap-3 mb-3">
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 mb-1">
                          <h3 className="font-semibold truncate">{item.name}</h3>
                          {item.category && (
                            <span className="text-xs px-2 py-0.5 rounded bg-primary/15 text-primary">
                              {item.category}
                            </span>
                          )}
                          <span className={`text-xs px-2 py-0.5 rounded ${
                            item.is_active 
                              ? 'bg-green-500/20 text-green-500' 
                              : 'bg-gray-500/20 text-gray-500'
                          }`}>
                            {item.is_active ? 'Активен' : 'Неактивен'}
                          </span>
                        </div>
                        <div className="flex items-center gap-3 text-sm text-muted-foreground">
                          <span>{item.amount}</span>
                          {item.is_slider ? (
                            <span className="font-semibold text-primary">{item.unit_price} ₽/{item.unit_name || 'ед.'}</span>
                          ) : (
                            <span className="font-semibold text-primary">{item.price} ₽</span>
                          )}
                          {item.is_slider && (
                            <span className="text-xs px-2 py-0.5 rounded bg-blue-500/15 text-blue-400">ползунок</span>
                          )}
                        </div>
                      </div>
                    </div>

                    <div className="space-y-2">
                      <div className="flex gap-2">
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={() => handleMoveShopItem(item, 'up')}
                          disabled={shopItems.sort((a, b) => a.order_position - b.order_position).findIndex(i => i.id === item.id) === 0}
                          title="Переместить вверх"
                        >
                          <Icon name="ArrowUp" size={14} />
                        </Button>
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={() => handleMoveShopItem(item, 'down')}
                          disabled={shopItems.sort((a, b) => a.order_position - b.order_position).findIndex(i => i.id === item.id) === shopItems.length - 1}
                          title="Переместить вниз"
                        >
                          <Icon name="ArrowDown" size={14} />
                        </Button>
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={() => handleEditShopItem(item)}
                          className="flex-1"
                        >
                          <Icon name="Edit" size={14} className="mr-1" />
                          Редактировать
                        </Button>
                      </div>
                      <div className="flex gap-2">
                        <Button
                          size="sm"
                          variant={item.is_active ? "outline" : "default"}
                          onClick={() => handleToggleActive(item)}
                          className="flex-1"
                        >
                          <Icon name={item.is_active ? "EyeOff" : "Eye"} size={14} className="mr-1" />
                          {item.is_active ? 'Скрыть' : 'Показать'}
                        </Button>
                        <Button
                          size="sm"
                          variant="destructive"
                          onClick={() => handleDeleteShopItem(item.id)}
                        >
                          <Icon name="Trash2" size={14} />
                        </Button>
                      </div>
                    </div>
                  </div>
                ))}
            </div>
          )}
        </Card>
      </div>
    </div>
  );
}