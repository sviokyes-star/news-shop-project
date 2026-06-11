import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import Icon from '@/components/ui/icon';
import { ShopFormData } from './ShopItemTypes';

interface ShopItemFormProps {
  formData: ShopFormData;
  editingId: number | null;
  error: string;
  success: string;
  onChange: (data: ShopFormData) => void;
  onSubmit: (e: React.FormEvent) => void;
  onCancel: () => void;
}

export default function ShopItemForm({ formData, editingId, error, success, onChange, onSubmit, onCancel }: ShopItemFormProps) {
  const set = (patch: Partial<ShopFormData>) => onChange({ ...formData, ...patch });

  return (
    <Card className="p-6 bg-card/80 backdrop-blur border-primary/20">
      <h2 className="text-2xl font-bold mb-6 flex items-center gap-2">
        <Icon name={editingId ? "Edit" : "Plus"} size={24} />
        {editingId ? 'Редактировать товар' : 'Добавить товар'}
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

      <form onSubmit={onSubmit} className="space-y-4">
        <div>
          <label className="block text-sm font-medium mb-2">Название товара</label>
          <Input
            value={formData.name}
            onChange={(e) => set({ name: e.target.value })}
            placeholder="VIP статус на месяц"
            required
          />
        </div>

        <div>
          <label className="block text-sm font-medium mb-2">Категория товара</label>
          <Input
            value={formData.category}
            onChange={(e) => set({ category: e.target.value })}
            placeholder="VIP, Скины, Бонусы..."
          />
        </div>

        <div>
          <label className="block text-sm font-medium mb-2">Количество/Описание</label>
          <Input
            value={formData.amount}
            onChange={(e) => set({ amount: e.target.value })}
            placeholder="1000 монет, 30 дней, и т.д."
          />
        </div>

        <div>
          <label className="block text-sm font-medium mb-2">Цена (₽)</label>
          <Input
            type="number"
            value={formData.price}
            onChange={(e) => set({ price: Number(e.target.value) })}
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
              checked={formData.is_slider}
              onChange={(e) => set({ is_slider: e.target.checked })}
              className="w-4 h-4"
            />
            <label htmlFor="is_slider" className="text-sm font-medium">
              Товар с ползунком (покупатель выбирает количество)
            </label>
          </div>

          {formData.is_slider && (
            <div className="space-y-3 pl-6 border-l-2 border-primary/30">
              <div>
                <label className="block text-sm font-medium mb-1">Название единицы (напр: фишка, монета)</label>
                <Input
                  value={formData.unit_name}
                  onChange={(e) => set({ unit_name: e.target.value })}
                  placeholder="фишка"
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">Цена за 1 единицу (₽)</label>
                <Input
                  type="number"
                  value={formData.unit_price}
                  onChange={(e) => set({ unit_price: Number(e.target.value) })}
                  placeholder="10"
                  min="1"
                />
              </div>
              <div className="grid grid-cols-3 gap-3">
                <div>
                  <label className="block text-sm font-medium mb-1">Мин.</label>
                  <Input
                    type="number"
                    value={formData.slider_min}
                    onChange={(e) => set({ slider_min: Number(e.target.value) })}
                    min="1"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium mb-1">Макс.</label>
                  <Input
                    type="number"
                    value={formData.slider_max}
                    onChange={(e) => set({ slider_max: Number(e.target.value) })}
                    min="1"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium mb-1">Шаг</label>
                  <Input
                    type="number"
                    value={formData.slider_step}
                    onChange={(e) => set({ slider_step: Number(e.target.value) })}
                    min="1"
                  />
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">Множитель (кол-во единиц товара за 1 ₽)</label>
                <Input
                  type="number"
                  value={formData.unit_multiplier}
                  onChange={(e) => set({ unit_multiplier: Number(e.target.value) })}
                  placeholder="1"
                  min="1"
                />
              </div>
              <p className="text-xs text-muted-foreground">
                Покупатель выберет от {formData.slider_min} до {formData.slider_max} ₽ с шагом {formData.slider_step} ₽.
                За каждый ₽ даётся {formData.unit_multiplier} {formData.unit_name || 'ед.'}.
                Итого за {formData.slider_min} ₽ → {formData.slider_min * formData.unit_multiplier} {formData.unit_name || 'ед.'}.
              </p>
            </div>
          )}
        </div>

        <div className="flex items-center gap-2">
          <input
            type="checkbox"
            id="is_active"
            checked={formData.is_active}
            onChange={(e) => set({ is_active: e.target.checked })}
            className="w-4 h-4"
          />
          <label htmlFor="is_active" className="text-sm font-medium">
            Активен (отображается в магазине)
          </label>
        </div>

        <div className="flex gap-3">
          <Button type="submit" className="flex-1">
            <Icon name={editingId ? "Save" : "Plus"} size={18} className="mr-2" />
            {editingId ? 'Сохранить' : 'Добавить'}
          </Button>
          {editingId && (
            <Button type="button" variant="outline" onClick={onCancel}>
              Отмена
            </Button>
          )}
        </div>
      </form>
    </Card>
  );
}