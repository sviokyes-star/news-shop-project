import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Slider } from '@/components/ui/slider';
import Icon from '@/components/ui/icon';
import { Product, SteamUser } from './types';

interface ProductCardProps {
  product: Product;
  user: SteamUser | null;
  sliderValues: Record<number, number>;
  setSliderValues: React.Dispatch<React.SetStateAction<Record<number, number>>>;
  purchasingItemId: number | null;
  delivering: { purchaseId: number; productId: number }[];
  handleBuy: (product: Product, quantity?: number) => void;
}

const ProductCard = ({
  product,
  user,
  sliderValues,
  setSliderValues,
  purchasingItemId,
  delivering,
  handleBuy,
}: ProductCardProps) => {
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
              <div className="flex items-center border border-border rounded overflow-hidden bg-background">
                <button
                  type="button"
                  className="px-2 py-1 text-muted-foreground hover:text-primary hover:bg-primary/10 transition-colors text-sm font-bold"
                  onClick={() => {
                    const step = multiplier > 1 ? product.slider_step * multiplier : product.slider_step;
                    const cur = multiplier > 1 ? totalUnits : qty;
                    let raw = cur - step;
                    if (multiplier > 1) raw = Math.round(raw / multiplier);
                    const clamped = Math.max(product.slider_min, Math.min(product.slider_max, raw));
                    setSliderValues(prev => ({ ...prev, [product.id]: clamped }));
                  }}
                >−</button>
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
                  className="w-16 text-xs font-semibold bg-transparent py-1 text-center focus:outline-none [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none"
                />
                <button
                  type="button"
                  className="px-2 py-1 text-muted-foreground hover:text-primary hover:bg-primary/10 transition-colors text-sm font-bold"
                  onClick={() => {
                    const step = multiplier > 1 ? product.slider_step * multiplier : product.slider_step;
                    const cur = multiplier > 1 ? totalUnits : qty;
                    let raw = cur + step;
                    if (multiplier > 1) raw = Math.round(raw / multiplier);
                    const clamped = Math.max(product.slider_min, Math.min(product.slider_max, raw));
                    setSliderValues(prev => ({ ...prev, [product.id]: clamped }));
                  }}
                >+</button>
              </div>
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
          {delivering.some(d => d.productId === product.id) ? (
            <Button
              size="sm"
              variant="secondary"
              className="h-8 px-3 text-xs cursor-default"
              disabled
            >
              <Icon name="Loader2" size={14} className="mr-1 animate-spin" />
              В процессе начисления
            </Button>
          ) : (
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
          )}
        </div>
      </div>
    </Card>
  );
};

export default ProductCard;
