import { Button } from '@/components/ui/button';
import Icon from '@/components/ui/icon';
import { ShopItem } from './ShopItemTypes';

interface ShopItemCardProps {
  item: ShopItem;
  isFirst: boolean;
  isLast: boolean;
  onEdit: (item: ShopItem) => void;
  onDelete: (id: number) => void;
  onToggleActive: (item: ShopItem) => void;
  onMove: (item: ShopItem, direction: 'up' | 'down') => void;
}

export default function ShopItemCard({ item, isFirst, isLast, onEdit, onDelete, onToggleActive, onMove }: ShopItemCardProps) {
  return (
    <div
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
            onClick={() => onMove(item, 'up')}
            disabled={isFirst}
            title="Переместить вверх"
          >
            <Icon name="ArrowUp" size={14} />
          </Button>
          <Button
            size="sm"
            variant="outline"
            onClick={() => onMove(item, 'down')}
            disabled={isLast}
            title="Переместить вниз"
          >
            <Icon name="ArrowDown" size={14} />
          </Button>
          <Button
            size="sm"
            variant="outline"
            onClick={() => onEdit(item)}
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
            onClick={() => onToggleActive(item)}
            className="flex-1"
          >
            <Icon name={item.is_active ? "EyeOff" : "Eye"} size={14} className="mr-1" />
            {item.is_active ? 'Скрыть' : 'Показать'}
          </Button>
          <Button
            size="sm"
            variant="destructive"
            onClick={() => onDelete(item.id)}
          >
            <Icon name="Trash2" size={14} />
          </Button>
        </div>
      </div>
    </div>
  );
}
