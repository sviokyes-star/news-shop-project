import { useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import Icon from '@/components/ui/icon';

interface LayoutProps {
  children: React.ReactNode;
}

const Layout = ({ children }: LayoutProps) => {
  const [isLoginOpen, setIsLoginOpen] = useState(false);
  const [isRegisterOpen, setIsRegisterOpen] = useState(false);
  const location = useLocation();

  return (
    <div className="min-h-screen bg-background">
      <nav className="border-b border-border backdrop-blur-xl bg-background/80 sticky top-0 z-50">
        <div className="container mx-auto px-4 sm:px-6 py-3 sm:py-5">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 bg-primary rounded-lg flex items-center justify-center">
                <Icon name="Gamepad2" size={24} className="text-primary-foreground" />
              </div>
              <h1 className="text-2xl font-bold tracking-tight">Okyes</h1>
            </div>

            <div className="flex items-center gap-6">
              <div className="flex gap-2 bg-card p-1.5 rounded-xl border border-border">
                <Link to="/">
                  <button
                    className={`px-6 py-2.5 rounded-lg transition-all duration-300 ${
                      location.pathname === '/'
                        ? 'bg-primary text-primary-foreground shadow-lg shadow-primary/20'
                        : 'text-muted-foreground hover:text-foreground hover:bg-secondary'
                    }`}
                  >
                    <div className="flex items-center gap-2">
                      <Icon name="Newspaper" size={18} />
                      <span className="font-medium">Новости</span>
                    </div>
                  </button>
                </Link>
                
                <Link to="/shop">
                  <button
                    className={`px-6 py-2.5 rounded-lg transition-all duration-300 ${
                      location.pathname === '/shop'
                        ? 'bg-primary text-primary-foreground shadow-lg shadow-primary/20'
                        : 'text-muted-foreground hover:text-foreground hover:bg-secondary'
                    }`}
                  >
                    <div className="flex items-center gap-2">
                      <Icon name="ShoppingBag" size={18} />
                      <span className="font-medium">Магазин</span>
                    </div>
                  </button>
                </Link>
              </div>

              <div className="flex items-center gap-3">
                <Dialog open={isLoginOpen} onOpenChange={setIsLoginOpen}>
                  <DialogTrigger asChild>
                    <Button variant="ghost" className="text-foreground hover:text-primary hover:bg-secondary">
                      Вход
                    </Button>
                  </DialogTrigger>
                  <DialogContent className="sm:max-w-md">
                    <DialogHeader>
                      <DialogTitle className="text-2xl font-bold">Вход в аккаунт</DialogTitle>
                      <DialogDescription>
                        Введите свои данные для входа в систему
                      </DialogDescription>
                    </DialogHeader>
                    <div className="space-y-6 py-4">
                      <div className="space-y-2">
                        <Label htmlFor="login-email">Email</Label>
                        <Input id="login-email" type="email" placeholder="game@example.com" className="h-11" />
                      </div>
                      <div className="space-y-2">
                        <Label htmlFor="login-password">Пароль</Label>
                        <Input id="login-password" type="password" placeholder="••••••••" className="h-11" />
                      </div>
                      <Button className="w-full h-11 text-base font-semibold shadow-lg shadow-primary/20">
                        <Icon name="LogIn" size={18} className="mr-2" />
                        Войти
                      </Button>
                    </div>
                  </DialogContent>
                </Dialog>

                <Dialog open={isRegisterOpen} onOpenChange={setIsRegisterOpen}>
                  <DialogTrigger asChild>
                    <Button className="shadow-lg shadow-primary/20 hover:shadow-xl hover:shadow-primary/30">
                      <Icon name="UserPlus" size={18} className="mr-2" />
                      Регистрация
                    </Button>
                  </DialogTrigger>
                  <DialogContent className="sm:max-w-md">
                    <DialogHeader>
                      <DialogTitle className="text-2xl font-bold">Создать аккаунт</DialogTitle>
                      <DialogDescription>
                        Зарегистрируйтесь для доступа ко всем функциям
                      </DialogDescription>
                    </DialogHeader>
                    <div className="space-y-6 py-4">
                      <div className="space-y-2">
                        <Label htmlFor="register-name">Имя игрока</Label>
                        <Input id="register-name" type="text" placeholder="Ваше имя" className="h-11" />
                      </div>
                      <div className="space-y-2">
                        <Label htmlFor="register-email">Email</Label>
                        <Input id="register-email" type="email" placeholder="game@example.com" className="h-11" />
                      </div>
                      <div className="space-y-2">
                        <Label htmlFor="register-password">Пароль</Label>
                        <Input id="register-password" type="password" placeholder="••••••••" className="h-11" />
                      </div>
                      <Button className="w-full h-11 text-base font-semibold shadow-lg shadow-primary/20">
                        <Icon name="UserPlus" size={18} className="mr-2" />
                        Зарегистрироваться
                      </Button>
                    </div>
                  </DialogContent>
                </Dialog>
              </div>
            </div>
          </div>
        </div>
      </nav>

      {children}
    </div>
  );
};

export default Layout;