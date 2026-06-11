import { Card } from '@/components/ui/card';
import Icon from '@/components/ui/icon';
import ShopItemCard from './ShopItemCard';
import { ShopItem } from './ShopItemTypes';

interface ShopItemListProps {
  shopItems: ShopItem[];
  isLoading: boolean;
  onEdit: (item: ShopItem) => void;
  onDelete: (id: number) => void;
  onToggleActive: (item: ShopItem) => void;
  onMove: (item: ShopItem, direction: 'up' | 'down') => void;
}

export default function ShopItemList({ shopItems, isLoading, onEdit, onDelete, onToggleActive, onMove }: ShopItemListProps) {
  const sorted = [...shopItems].sort((a, b) => a.order_position - b.order_position);

  return (
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
          {sorted.map((item, index) => (
            <ShopItemCard
              key={item.id}
              item={item}
              isFirst={index === 0}
              isLast={index === sorted.length - 1}
              onEdit={onEdit}
              onDelete={onDelete}
              onToggleActive={onToggleActive}
              onMove={onMove}
            />
          ))}
        </div>
      )}
    </Card>
  );
}
