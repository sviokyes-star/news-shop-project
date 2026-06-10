import { Card } from '@/components/ui/card';
import Icon from '@/components/ui/icon';

interface UserStatsProps {
  totalUsers: number;
  totalBalance: number;
  moderatorsCount: number;
  blockedCount: number;
}

export default function UserStats({ 
  totalUsers, 
  totalBalance, 
  moderatorsCount, 
  blockedCount 
}: UserStatsProps) {
  const stats = [
    { icon: 'Users', color: 'text-primary', bg: 'bg-primary/15', label: 'Пользователей', value: totalUsers },
    { icon: 'Wallet', color: 'text-green-500', bg: 'bg-green-500/15', label: 'Общий баланс', value: `${totalBalance} ₽` },
    { icon: 'Shield', color: 'text-blue-500', bg: 'bg-blue-500/15', label: 'Модераторов', value: moderatorsCount },
    { icon: 'Ban', color: 'text-red-500', bg: 'bg-red-500/15', label: 'Заблокировано', value: blockedCount },
  ];

  return (
    <Card className="p-3 bg-card/80 backdrop-blur border-primary/20">
      <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
        {stats.map(s => (
          <div key={s.label} className="flex items-center gap-2.5">
            <div className={`w-8 h-8 rounded-lg ${s.bg} flex items-center justify-center flex-shrink-0`}>
              <Icon name={s.icon as never} size={16} className={s.color} />
            </div>
            <div>
              <p className="text-xs text-muted-foreground leading-none mb-0.5">{s.label}</p>
              <p className="text-lg font-bold leading-none">{s.value}</p>
            </div>
          </div>
        ))}
      </div>
    </Card>
  );
}
