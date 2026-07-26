import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import Icon from '@/components/ui/icon';
import { Transaction, formatDate } from './types';

interface BalanceCardProps {
  balance: number;
  isLoadingBalance: boolean;
  isHistoryOpen: boolean;
  setIsHistoryOpen: (open: boolean) => void;
  loadHistory: () => void;
  isLoadingHistory: boolean;
  history: Transaction[];
  isTopUpDialogOpen: boolean;
  setIsTopUpDialogOpen: (open: boolean) => void;
  isCreatingPayment: boolean;
  customAmount: string;
  setCustomAmount: React.Dispatch<React.SetStateAction<string>>;
  handleTopUp: (productId?: number) => void;
  loadBalance: () => void;
}

const BalanceCard = ({
  balance,
  isLoadingBalance,
  isHistoryOpen,
  setIsHistoryOpen,
  loadHistory,
  isLoadingHistory,
  history,
  isTopUpDialogOpen,
  setIsTopUpDialogOpen,
  isCreatingPayment,
  customAmount,
  setCustomAmount,
  handleTopUp,
  loadBalance,
}: BalanceCardProps) => {
  return (
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
          <Dialog open={isHistoryOpen} onOpenChange={(open) => { setIsHistoryOpen(open); if (open) loadHistory(); }}>
            <DialogTrigger asChild>
              <Button variant="outline" size="lg" className="gap-2">
                <Icon name="ReceiptText" size={18} />
                История
              </Button>
            </DialogTrigger>
            <DialogContent className="sm:max-w-2xl max-h-[80vh] flex flex-col">
              <DialogHeader>
                <DialogTitle>История операций</DialogTitle>
                <DialogDescription>Пополнения и расходы баланса</DialogDescription>
              </DialogHeader>
              <div className="overflow-y-auto flex-1 -mx-6 px-6">
                {isLoadingHistory ? (
                  <div className="py-8 text-center text-muted-foreground">
                    <Icon name="Loader2" size={28} className="animate-spin mx-auto mb-2" />
                    Загрузка...
                  </div>
                ) : history.length === 0 ? (
                  <div className="py-8 text-center text-muted-foreground">
                    <Icon name="ReceiptText" size={28} className="mx-auto mb-2" />
                    Операций пока нет
                  </div>
                ) : (
                  <div className="divide-y divide-border">
                    {history.map((tx) => {
                      const isIncome = tx.amount > 0;
                      return (
                        <div key={tx.id} className="flex items-center gap-3 py-3">
                          <div className={`w-7 h-7 rounded-full flex items-center justify-center flex-shrink-0 ${isIncome ? 'bg-green-500/15' : 'bg-red-500/15'}`}>
                            <Icon name={isIncome ? 'ArrowDownLeft' : 'ArrowUpRight'} size={14} className={isIncome ? 'text-green-500' : 'text-red-400'} />
                          </div>
                          <div className="flex-1 min-w-0">
                            <p className="text-sm font-medium truncate">{tx.description || tx.type}</p>
                            <p className="text-xs text-muted-foreground">{formatDate(tx.created_at)}</p>
                          </div>
                          <span className={`text-sm font-bold whitespace-nowrap ${isIncome ? 'text-green-500' : 'text-red-400'}`}>
                            {isIncome ? '+' : ''}{tx.amount} ₽
                          </span>
                        </div>
                      );
                    })}
                  </div>
                )}
              </div>
            </DialogContent>
          </Dialog>

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
                  <div className="flex items-center border border-border rounded-md overflow-hidden bg-background">
                    <button
                      type="button"
                      className="px-3 py-2 text-muted-foreground hover:text-primary hover:bg-primary/10 transition-colors text-lg font-bold select-none"
                      onClick={() => setCustomAmount(v => String(Math.max(10, (Number(v) || 10) - 10)))}
                    >−</button>
                    <input
                      id="custom-amount"
                      type="number"
                      placeholder="Минимум 10 ₽"
                      value={customAmount}
                      onChange={(e) => setCustomAmount(e.target.value)}
                      min="10"
                      className="flex-1 bg-transparent text-center text-sm py-2 focus:outline-none [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none"
                    />
                    <button
                      type="button"
                      className="px-3 py-2 text-muted-foreground hover:text-primary hover:bg-primary/10 transition-colors text-lg font-bold select-none"
                      onClick={() => setCustomAmount(v => String((Number(v) || 0) + 10))}
                    >+</button>
                  </div>
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
  );
};

export default BalanceCard;
