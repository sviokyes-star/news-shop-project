import { Product, SteamUser } from './types';
import ProductCard from './ProductCard';

interface ProductListProps {
  products: Product[];
  user: SteamUser | null;
  sliderValues: Record<number, number>;
  setSliderValues: React.Dispatch<React.SetStateAction<Record<number, number>>>;
  purchasingItemId: number | null;
  delivering: { purchaseId: number; productId: number }[];
  handleBuy: (product: Product, quantity?: number) => void;
}

const ProductList = ({
  products,
  user,
  sliderValues,
  setSliderValues,
  purchasingItemId,
  delivering,
  handleBuy,
}: ProductListProps) => {
  return (
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
              {items.map(product => (
                <ProductCard
                  key={product.id}
                  product={product}
                  user={user}
                  sliderValues={sliderValues}
                  setSliderValues={setSliderValues}
                  purchasingItemId={purchasingItemId}
                  delivering={delivering}
                  handleBuy={handleBuy}
                />
              ))}
            </div>
          </div>
        ));
      })()}
    </div>
  );
};

export default ProductList;
