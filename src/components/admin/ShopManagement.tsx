import { useState, useEffect } from 'react';
import func2url from '../../../backend/func2url.json';
import ShopItemForm from './ShopItemForm';
import ShopItemList from './ShopItemList';
import { ShopItem, SteamUser, ShopFormData, EMPTY_FORM } from './ShopItemTypes';

interface ShopManagementProps {
  shopItems: ShopItem[];
  isLoading: boolean;
  onRefresh: () => Promise<void>;
}

export default function ShopManagement({ shopItems, isLoading, onRefresh }: ShopManagementProps) {
  const [user, setUser] = useState<SteamUser | null>(null);
  const [editingShopId, setEditingShopId] = useState<number | null>(null);
  const [error, setError] = useState<string>('');
  const [success, setSuccess] = useState<string>('');
  const [shopFormData, setShopFormData] = useState<ShopFormData>(EMPTY_FORM);

  useEffect(() => {
    const savedUser = localStorage.getItem('steamUser');
    if (savedUser) setUser(JSON.parse(savedUser));
  }, []);

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
      const body = editingShopId ? { ...shopFormData, id: editingShopId } : shopFormData;

      console.log('📤 Sending request:', { url, method, body });

      const response = await fetch(url, {
        method,
        headers: { 'Content-Type': 'application/json', 'X-Admin-Steam-Id': user.steamId },
        body: JSON.stringify(body)
      });

      console.log('📥 Response:', response.status, response.ok);

      if (response.ok) {
        setSuccess(editingShopId ? 'Товар обновлён' : 'Товар добавлен');
        await onRefresh();
        setTimeout(() => resetShopForm(), 1000);
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
      unit_name: item.unit_name || '',
      unit_multiplier: item.unit_multiplier ?? 1
    });
  };

  const handleDeleteShopItem = async (id: number) => {
    if (!confirm('Удалить этот товар?')) return;
    if (!user) return;

    try {
      const response = await fetch(`${func2url['shop-items']}?id=${id}`, {
        method: 'DELETE',
        headers: { 'X-Admin-Steam-Id': user.steamId }
      });
      if (response.ok) await onRefresh();
    } catch (error) {
      console.error('Failed to delete shop item:', error);
    }
  };

  const handleToggleActive = async (item: ShopItem) => {
    if (!user) return;

    try {
      const response = await fetch(func2url['shop-items'], {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json', 'X-Admin-Steam-Id': user.steamId },
        body: JSON.stringify({ id: item.id, is_active: !item.is_active })
      });
      if (response.ok) await onRefresh();
    } catch (error) {
      console.error('Failed to toggle shop item:', error);
    }
  };

  const resetShopForm = () => {
    setEditingShopId(null);
    setShopFormData(EMPTY_FORM);
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
        headers: { 'Content-Type': 'application/json', 'X-Admin-Steam-Id': user.steamId },
        body: JSON.stringify({ id: item.id, order_position: originalSwapPos })
      });
      console.log('✅ Response 1:', response1.status);

      const response2 = await fetch(func2url['shop-items'], {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json', 'X-Admin-Steam-Id': user.steamId },
        body: JSON.stringify({ id: swapItem.id, order_position: originalItemPos })
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
        <ShopItemForm
          formData={shopFormData}
          editingId={editingShopId}
          error={error}
          success={success}
          onChange={setShopFormData}
          onSubmit={handleShopSubmit}
          onCancel={resetShopForm}
        />
      </div>
      <div>
        <ShopItemList
          shopItems={shopItems}
          isLoading={isLoading}
          onEdit={handleEditShopItem}
          onDelete={handleDeleteShopItem}
          onToggleActive={handleToggleActive}
          onMove={handleMoveShopItem}
        />
      </div>
    </div>
  );
}