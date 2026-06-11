import { useState } from 'react';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import Icon from '@/components/ui/icon';

export type BanType = 'delete_only' | 'ban_60' | 'ban_permanent';

interface BanDialogProps {
  open: boolean;
  onClose: () => void;
  onConfirm: (banType: BanType, reason: string) => void;
  isLoading?: boolean;
}

const options: { type: BanType; label: string; description: string; icon: string; variant: 'outline' | 'destructive'; needsReason: boolean }[] = [
  {
    type: 'delete_only',
    label: 'Просто удалить',
    description: 'Удалить сообщение без блокировки пользователя',
    icon: 'Trash2',
    variant: 'outline',
    needsReason: false,
  },
  {
    type: 'ban_60',
    label: 'Заблокировать на 60 минут',
    description: 'Удалить сообщение и заблокировать на 1 час',
    icon: 'Clock',
    variant: 'destructive',
    needsReason: true,
  },
  {
    type: 'ban_permanent',
    label: 'Заблокировать навсегда',
    description: 'Удалить сообщение и заблокировать навсегда',
    icon: 'Ban',
    variant: 'destructive',
    needsReason: true,
  },
];

export default function BanDialog({ open, onClose, onConfirm, isLoading }: BanDialogProps) {
  const [selected, setSelected] = useState<BanType | null>(null);
  const [reason, setReason] = useState('');

  const selectedOption = options.find(o => o.type === selected);

  const handleClose = () => {
    setSelected(null);
    setReason('');
    onClose();
  };

  const handleConfirm = () => {
    if (!selected) return;
    onConfirm(selected, reason.trim());
    setSelected(null);
    setReason('');
  };

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="max-w-sm">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Icon name="ShieldAlert" size={18} className="text-destructive" />
            Удалить сообщение
          </DialogTitle>
        </DialogHeader>
        <div className="flex flex-col gap-2 pt-1">
          {!selected ? (
            <>
              {options.map(opt => (
                <Button
                  key={opt.type}
                  variant={opt.variant}
                  className="flex flex-col items-start h-auto py-3 px-4 gap-0.5"
                  disabled={isLoading}
                  onClick={() => {
                    if (opt.needsReason) {
                      setSelected(opt.type);
                    } else {
                      onConfirm(opt.type, '');
                    }
                  }}
                >
                  <span className="flex items-center gap-2 font-semibold">
                    <Icon name={opt.icon as 'Trash2'} size={15} />
                    {opt.label}
                  </span>
                  <span className="text-xs font-normal opacity-75 text-left">{opt.description}</span>
                </Button>
              ))}
              <Button variant="ghost" size="sm" onClick={handleClose} disabled={isLoading}>
                Отмена
              </Button>
            </>
          ) : (
            <>
              <div className="flex items-center gap-2 px-1 py-1 text-sm text-muted-foreground">
                <Icon name={selectedOption?.icon as 'Ban'} size={14} className="text-destructive" />
                <span className="font-medium text-foreground">{selectedOption?.label}</span>
              </div>
              <div className="space-y-1.5">
                <label className="text-sm font-medium">Причина блокировки</label>
                <Input
                  placeholder="Например: оскорбления, спам..."
                  value={reason}
                  onChange={e => setReason(e.target.value)}
                  onKeyDown={e => { if (e.key === 'Enter') handleConfirm(); }}
                  autoFocus
                  maxLength={200}
                />
                <p className="text-xs text-muted-foreground">Необязательно — будет отображаться в профиле пользователя</p>
              </div>
              <div className="flex gap-2 pt-1">
                <Button variant="destructive" className="flex-1" onClick={handleConfirm} disabled={isLoading}>
                  {isLoading ? <Icon name="Loader2" size={14} className="animate-spin mr-1" /> : null}
                  Подтвердить
                </Button>
                <Button variant="outline" onClick={() => { setSelected(null); setReason(''); }} disabled={isLoading}>
                  Назад
                </Button>
              </div>
            </>
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
}
