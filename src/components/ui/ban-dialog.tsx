import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import Icon from '@/components/ui/icon';

export type BanType = 'delete_only' | 'ban_60' | 'ban_permanent';

interface BanDialogProps {
  open: boolean;
  onClose: () => void;
  onConfirm: (banType: BanType) => void;
  isLoading?: boolean;
}

const options: { type: BanType; label: string; description: string; icon: string; variant: 'outline' | 'destructive' }[] = [
  {
    type: 'delete_only',
    label: 'Просто удалить',
    description: 'Удалить сообщение без блокировки пользователя',
    icon: 'Trash2',
    variant: 'outline',
  },
  {
    type: 'ban_60',
    label: 'Заблокировать на 60 минут',
    description: 'Удалить сообщение и заблокировать на 1 час',
    icon: 'Clock',
    variant: 'destructive',
  },
  {
    type: 'ban_permanent',
    label: 'Заблокировать навсегда',
    description: 'Удалить сообщение и заблокировать навсегда',
    icon: 'Ban',
    variant: 'destructive',
  },
];

export default function BanDialog({ open, onClose, onConfirm, isLoading }: BanDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onClose}>
      <DialogContent className="max-w-sm">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Icon name="ShieldAlert" size={18} className="text-destructive" />
            Удалить сообщение
          </DialogTitle>
        </DialogHeader>
        <div className="flex flex-col gap-2 pt-1">
          {options.map(opt => (
            <Button
              key={opt.type}
              variant={opt.variant}
              className="flex flex-col items-start h-auto py-3 px-4 gap-0.5"
              disabled={isLoading}
              onClick={() => onConfirm(opt.type)}
            >
              <span className="flex items-center gap-2 font-semibold">
                <Icon name={opt.icon as 'Trash2'} size={15} />
                {opt.label}
              </span>
              <span className="text-xs font-normal opacity-75 text-left">{opt.description}</span>
            </Button>
          ))}
          <Button variant="ghost" size="sm" onClick={onClose} disabled={isLoading}>
            Отмена
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  );
}