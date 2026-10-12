import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Button } from '@/components/ui/button';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import Icon from '@/components/ui/icon';
import func2url from '@/lib/api';
import NotificationBell from '@/components/NotificationBell';
import { Sheet, SheetContent, SheetHeader, SheetTitle, SheetTrigger } from '@/components/ui/sheet';

interface SteamUser {
  steamId: string;
  personaName: string;
  avatarUrl: string;
  profileUrl: string;
}

interface MenuItem {
  id: number;
  name: string;
  label: string;
  route: string;
  icon: string;
  isVisible: boolean;
  orderPosition: number;
}

interface NavigationProps {
  activeTab: 'news' | 'shop' | 'servers' | 'tournaments' | 'partners';
  setActiveTab: (tab: 'news' | 'shop' | 'servers' | 'tournaments' | 'partners') => void;
  user: SteamUser | null;
  isLoginOpen: boolean;
  setIsLoginOpen: (open: boolean) => void;
  isRegisterOpen: boolean;
  setIsRegisterOpen: (open: boolean) => void;
  handleSteamLogin: () => void;
  handleLogout: () => void;
}

const DEFAULT_LOGO = 'https://cdn.poehali.dev/projects/0cd5ea72-8c09-43b2-b92c-a0fdee84371e/files/favicon-1771578220171.png';

const Navigation = ({
  activeTab,
  setActiveTab,
  user,
  isLoginOpen,
  setIsLoginOpen,
  isRegisterOpen,
  setIsRegisterOpen,
  handleSteamLogin,
  handleLogout
}: NavigationProps) => {
  const navigate = useNavigate();
  const [menuItems, setMenuItems] = useState<MenuItem[]>([]);
  const [mobileOpen, setMobileOpen] = useState(false);
  const [logoUrl, setLogoUrl] = useState<string>(localStorage.getItem('site_logo_url') || DEFAULT_LOGO);

  useEffect(() => {
    loadMenuItems();
    loadLogo();
  }, []);

  const loadMenuItems = async () => {
    try {
      const response = await fetch(func2url['menu-items']);
      const data = await response.json();
      const visibleItems = (data.menuItems || []).filter((item: MenuItem) => item.isVisible);
      setMenuItems(visibleItems);
    } catch (error) {
      console.error('Failed to load menu items:', error);
    }
  };

  const loadLogo = async () => {
    try {
      const res = await fetch(func2url['site-settings']);
      const data = await res.json();
      if (data.logo_url) {
        setLogoUrl(data.logo_url);
        localStorage.setItem('site_logo_url', data.logo_url);
      }
    } catch {
      // fallback to default
    }
  };

  return (
    <nav className="border-b neon-border backdrop-blur-xl bg-background/80 sticky top-0 z-50">
      <div className="container mx-auto px-4 sm:px-6 py-3 lg:py-5">
        <div className="flex items-center justify-between gap-3">
          <div className="flex items-center gap-2 sm:gap-3 cursor-pointer shrink-0" onClick={() => navigate('/')}>
            <img
              src={logoUrl}
              alt="Okyes"
              className="w-10 h-10 sm:w-12 sm:h-12 lg:w-16 lg:h-16 rounded-lg"
            />
            <h1 className="text-xl lg:text-2xl font-bold tracking-tight neon-glow">Okyes</h1>
          </div>

          <div className="flex items-center gap-2 lg:gap-6">
            <div className="hidden lg:flex gap-2 bg-card p-1.5 rounded-xl border border-border">
              {menuItems.map((item) => (
                <button
                  key={item.id}
                  onClick={() => navigate(item.route)}
                  className={`px-4 xl:px-6 py-2.5 rounded-lg transition-all duration-300 ${
                    activeTab === item.name
                      ? 'bg-primary text-primary-foreground neon-button'
                      : 'text-muted-foreground hover:text-foreground hover:bg-secondary'
                  }`}
                >
                  <div className="flex items-center gap-2">
                    <Icon name={item.icon as any} size={18} />
                    <span className="font-medium">{item.label}</span>
                  </div>
                </button>
              ))}
            </div>

            {user ? (
              <div className="flex items-center gap-1">
                <NotificationBell steamId={user.steamId} />
              <DropdownMenu modal={false}>
                <DropdownMenuTrigger asChild>
                  <button className="flex items-center gap-2 sm:gap-3 px-2 sm:px-4 py-2 rounded-lg hover:bg-secondary transition-colors">
                    <img src={user.avatarUrl} alt={user.personaName} className="w-8 h-8 rounded-full" />
                    <span className="font-medium hidden sm:inline max-w-[140px] truncate">{user.personaName}</span>
                    <Icon name="ChevronDown" size={16} className="text-muted-foreground hidden sm:block" />
                  </button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="w-56">
                  <DropdownMenuItem onClick={() => navigate('/profile')} className="cursor-pointer">
                    <Icon name="User" size={16} className="mr-2" />
                    Профиль
                  </DropdownMenuItem>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem onClick={handleLogout} className="cursor-pointer text-destructive focus:text-destructive">
                    <Icon name="LogOut" size={16} className="mr-2" />
                    Выйти
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
              </div>
            ) : (
              <div className="flex gap-2 sm:gap-3">
                <Dialog open={isLoginOpen} onOpenChange={setIsLoginOpen}>
                  <DialogTrigger asChild>
                    <Button variant="outline" className="gap-2 px-3 sm:px-4">
                      <Icon name="LogIn" size={18} />
                      <span className="hidden sm:inline">Войти</span>
                    </Button>
                  </DialogTrigger>
                  <DialogContent className="sm:max-w-md">
                    <DialogHeader>
                      <DialogTitle>Вход через Steam</DialogTitle>
                      <DialogDescription>
                        Войдите используя свой Steam аккаунт
                      </DialogDescription>
                    </DialogHeader>
                    <div className="space-y-4">
                      <Button 
                        onClick={handleSteamLogin}
                        className="w-full gap-2 bg-[#171a21] hover:bg-[#1b2838]"
                      >
                        <Icon name="Gamepad2" size={18} />
                        Войти через Steam
                      </Button>
                    </div>
                  </DialogContent>
                </Dialog>

                <Dialog open={isRegisterOpen} onOpenChange={setIsRegisterOpen}>
                  <DialogTrigger asChild>
                    <Button className="gap-2 hidden sm:inline-flex">
                      <Icon name="UserPlus" size={18} />
                      Регистрация
                    </Button>
                  </DialogTrigger>
                  <DialogContent className="sm:max-w-md">
                    <DialogHeader>
                      <DialogTitle>Регистрация</DialogTitle>
                      <DialogDescription>
                        Создайте аккаунт используя Steam
                      </DialogDescription>
                    </DialogHeader>
                    <div className="space-y-4">
                      <Button 
                        onClick={handleSteamLogin}
                        className="w-full gap-2 bg-[#171a21] hover:bg-[#1b2838]"
                      >
                        <Icon name="Gamepad2" size={18} />
                        Регистрация через Steam
                      </Button>
                    </div>
                  </DialogContent>
                </Dialog>
              </div>
            )}

            <Sheet open={mobileOpen} onOpenChange={setMobileOpen}>
              <SheetTrigger asChild>
                <Button variant="outline" size="icon" className="lg:hidden shrink-0" aria-label="Меню">
                  <Icon name="Menu" size={20} />
                </Button>
              </SheetTrigger>
              <SheetContent side="right" className="w-[280px] sm:w-[320px] p-0">
                <SheetHeader className="p-5 border-b border-border">
                  <SheetTitle className="flex items-center gap-3">
                    <img src={logoUrl} alt="Okyes" className="w-9 h-9 rounded-lg" />
                    <span className="neon-glow">Okyes</span>
                  </SheetTitle>
                </SheetHeader>
                <div className="flex flex-col gap-1 p-3">
                  {menuItems.map((item) => (
                    <button
                      key={item.id}
                      onClick={() => { navigate(item.route); setMobileOpen(false); }}
                      className={`flex items-center gap-3 px-4 py-3 rounded-lg text-left transition-colors ${
                        activeTab === item.name
                          ? 'bg-primary text-primary-foreground'
                          : 'text-muted-foreground hover:text-foreground hover:bg-secondary'
                      }`}
                    >
                      <Icon name={item.icon as any} size={20} />
                      <span className="font-medium">{item.label}</span>
                    </button>
                  ))}
                </div>
                <div className="p-3 border-t border-border">
                  {user ? (
                    <div className="flex flex-col gap-1">
                      <button
                        onClick={() => { navigate('/profile'); setMobileOpen(false); }}
                        className="flex items-center gap-3 px-4 py-3 rounded-lg hover:bg-secondary text-left"
                      >
                        <img src={user.avatarUrl} alt={user.personaName} className="w-7 h-7 rounded-full" />
                        <span className="font-medium truncate">{user.personaName}</span>
                      </button>
                      <button
                        onClick={() => { handleLogout(); setMobileOpen(false); }}
                        className="flex items-center gap-3 px-4 py-3 rounded-lg hover:bg-secondary text-destructive text-left"
                      >
                        <Icon name="LogOut" size={20} />
                        <span className="font-medium">Выйти</span>
                      </button>
                    </div>
                  ) : (
                    <Button
                      onClick={() => { setMobileOpen(false); handleSteamLogin(); }}
                      className="w-full gap-2"
                    >
                      <Icon name="Gamepad2" size={18} />
                      Войти через Steam
                    </Button>
                  )}
                </div>
              </SheetContent>
            </Sheet>
          </div>
        </div>
      </div>
    </nav>
  );
};

export default Navigation;