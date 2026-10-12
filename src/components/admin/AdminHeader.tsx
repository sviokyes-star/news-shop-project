import { useNavigate } from 'react-router-dom';
import Icon from '@/components/ui/icon';

interface AdminHeaderProps {
  user: {
    personaName: string;
    avatarUrl: string;
  };
}

export default function AdminHeader({ user }: AdminHeaderProps) {
  const navigate = useNavigate();

  return (
    <div className="border-b border-border backdrop-blur-xl bg-background/80 sticky top-0 z-50">
      <div className="container mx-auto px-4 sm:px-6 py-3 sm:py-5">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3 cursor-pointer" onClick={() => navigate('/')}>
            <div className="w-10 h-10 bg-primary rounded-lg flex items-center justify-center">
              <Icon name="Gamepad2" size={24} className="text-primary-foreground" />
            </div>
            <h1 className="text-xl sm:text-2xl font-bold tracking-tight">Okyes Admin</h1>
          </div>

          <div className="flex items-center gap-3">
            <button
              onClick={() => navigate('/profile')}
              className="flex items-center gap-3 px-4 py-2 rounded-lg hover:bg-secondary transition-colors"
            >
              <img src={user.avatarUrl} alt={user.personaName} className="w-8 h-8 rounded-full" />
              <span className="font-medium hidden sm:block">{user.personaName}</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
