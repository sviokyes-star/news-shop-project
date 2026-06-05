import { useLocation, useNavigate } from "react-router-dom";
import { useEffect } from "react";
import { Button } from "@/components/ui/button";
import Icon from "@/components/ui/icon";

const NotFoundPage = () => {
  const location = useLocation();
  const navigate = useNavigate();

  useEffect(() => {
    console.error("404 Error: User attempted to access non-existent route:", location.pathname);
  }, [location.pathname]);

  return (
    <div className="min-h-screen bg-gradient-to-br from-background via-background to-primary/5 flex items-center justify-center relative overflow-hidden">
      <div className="absolute inset-0 pointer-events-none">
        <div className="absolute top-1/4 left-1/4 w-96 h-96 bg-primary/5 rounded-full blur-3xl" />
        <div className="absolute bottom-1/4 right-1/4 w-64 h-64 bg-primary/10 rounded-full blur-3xl" />
      </div>

      <div className="relative text-center space-y-8 px-6">
        <div className="relative py-8">
          <div className="text-[12rem] font-black leading-none tracking-tighter text-primary/10 select-none absolute inset-0 flex items-center justify-center">
            404
          </div>
          <div className="text-[8rem] font-black leading-none tracking-tighter bg-gradient-to-br from-primary to-primary/40 bg-clip-text text-transparent relative">
            404
          </div>
        </div>

        <div className="space-y-3">
          <h1 className="text-3xl font-bold tracking-tight">Страница не найдена</h1>
          <p className="text-muted-foreground text-lg max-w-md mx-auto">
            Эта страница улетела в открытый космос. Мы не можем её найти.
          </p>
        </div>

        <div className="inline-block px-4 py-1.5 bg-primary/10 border border-primary/20 rounded-full">
          <span className="text-sm font-mono text-primary">{location.pathname}</span>
        </div>

        <div className="flex items-center justify-center gap-3 flex-wrap">
          <Button onClick={() => navigate(-1)} variant="outline" className="gap-2">
            <Icon name="ArrowLeft" size={16} />
            Назад
          </Button>
          <Button onClick={() => navigate("/")} className="gap-2">
            <Icon name="Home" size={16} />
            На главную
          </Button>
        </div>
      </div>
    </div>
  );
};

export default NotFoundPage;
