import { useState, useEffect } from 'react';
import { Card } from '@/components/ui/card';
import Icon from '@/components/ui/icon';
import func2url from '../../../backend/func2url.json';

interface Transaction {
  id: number;
  amount: number;
  type: string;
  description: string;
  created_at: string;
}

interface ProfileHistoryProps {
  steamId: string;
}

const TYPE_LABELS: Record<string, string> = {
  purchase: 'Покупка',
  deposit: 'Пополнение',
  payment: 'Пополнение',
  refund: 'Возврат',
};

function formatDate(iso: string) {
  const d = new Date(iso);
  return d.toLocaleString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
}

export default function ProfileHistory({ steamId }: ProfileHistoryProps) {
  const [history, setHistory] = useState<Transaction[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const load = async () => {
      setIsLoading(true);
      try {
        const res = await fetch(`${func2url.balance}?steam_id=${steamId}&action=history`);
        const data = await res.json();
        setHistory(data.history || []);
      } catch (e) {
        console.error('Failed to load history', e);
      } finally {
        setIsLoading(false);
      }
    };
    load();
  }, [steamId]);

  return (
    <div className="space-y-4">
      <h2 className="text-2xl font-bold">
        История операций{' '}
        <span className="text-muted-foreground text-lg font-normal">({history.length})</span>
      </h2>

      {isLoading ? (
        <Card className="p-8 text-center border border-dashed border-border bg-card/30">
          <Icon name="Loader2" size={36} className="text-muted-foreground mx-auto mb-2 animate-spin" />
          <p className="text-muted-foreground">Загрузка...</p>
        </Card>
      ) : history.length === 0 ? (
        <Card className="p-8 text-center border border-dashed border-border bg-card/30">
          <Icon name="ReceiptText" size={36} className="text-muted-foreground mx-auto mb-2" />
          <p className="text-muted-foreground">Операций пока нет</p>
        </Card>
      ) : (
        <Card className="border border-border bg-card/50 divide-y divide-border overflow-hidden">
          {history.map((tx) => {
            const isIncome = tx.amount > 0;
            return (
              <div key={tx.id} className="flex items-center gap-4 px-5 py-3 hover:bg-secondary/30 transition-colors">
                <div className={`w-8 h-8 rounded-full flex items-center justify-center flex-shrink-0 ${
                  isIncome ? 'bg-green-500/15' : 'bg-red-500/15'
                }`}>
                  <Icon
                    name={isIncome ? 'ArrowDownLeft' : 'ArrowUpRight'}
                    size={16}
                    className={isIncome ? 'text-green-500' : 'text-red-400'}
                  />
                </div>

                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium truncate">
                    {tx.description || TYPE_LABELS[tx.type] || tx.type}
                  </p>
                  <p className="text-xs text-muted-foreground">{formatDate(tx.created_at)}</p>
                </div>

                <span className={`text-sm font-bold whitespace-nowrap ${
                  isIncome ? 'text-green-500' : 'text-red-400'
                }`}>
                  {isIncome ? '+' : ''}{tx.amount} ₽
                </span>
              </div>
            );
          })}
        </Card>
      )}
    </div>
  );
}
