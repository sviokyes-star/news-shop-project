import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import Icon from '@/components/ui/icon';

interface AdminAccessDeniedProps {
  user: {
    steamId: string;
    personaName: string;
    avatarUrl: string;
    profileUrl: string;
  } | null;
  onSteamLogin: () => void;
}

export default function AdminAccessDenied({ user, onSteamLogin }: AdminAccessDeniedProps) {
  return (
    <div className="min-h-screen bg-gradient-to-br from-background via-background to-primary/5 flex items-center justify-center p-4">
      <Card className="p-5 sm:p-8 bg-card/80 backdrop-blur border-primary/20 max-w-md w-full">
        <div className="text-center space-y-6">
          <div className="w-16 h-16 bg-destructive/10 rounded-full flex items-center justify-center mx-auto">
            <Icon name="ShieldAlert" size={32} className="text-destructive" />
          </div>
          
          <div className="space-y-2">
            <h2 className="text-2xl font-bold">Доступ ограничен</h2>
            <p className="text-muted-foreground">
              {user 
                ? 'У вас нет прав администратора для доступа к этой странице'
                : 'Войдите через Steam для проверки прав доступа'
              }
            </p>
            {user && (
              <p className="text-sm text-muted-foreground mt-4">
                Steam ID: {user.steamId}
              </p>
            )}
          </div>

          {!user && (
            <Button onClick={onSteamLogin} className="w-full">
              <Icon name="User" size={18} className="mr-2" />
              Войти через Steam
            </Button>
          )}
        </div>
      </Card>
    </div>
  );
}
