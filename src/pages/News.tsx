import { useState, useEffect } from 'react';
import { Card } from '@/components/ui/card';
import Icon from '@/components/ui/icon';
import func2url from '@/lib/api';
import { formatShortDate } from '@/utils/dateFormat';

interface NewsItem {
  id: number;
  title: string;
  description: string;
  date: string;
}

interface SteamUser {
  steamId: string;
  personaName: string;
  avatarUrl: string;
  profileUrl: string;
}

const News = () => {
  const [isLoginOpen, setIsLoginOpen] = useState(false);
  const [isRegisterOpen, setIsRegisterOpen] = useState(false);
  const [user, setUser] = useState<SteamUser | null>(null);
  const [newsItems, setNewsItems] = useState<NewsItem[]>([]);

  useEffect(() => {
    const savedUser = localStorage.getItem('steamUser');
    if (savedUser) {
      setUser(JSON.parse(savedUser));
    }

    loadNews();

    const params = new URLSearchParams(window.location.search);
    const claimedId = params.get('openid.claimed_id');
    
    if (claimedId) {
      const verifyParams = new URLSearchParams();
      params.forEach((value, key) => {
        verifyParams.append(key, value);
      });
      verifyParams.append('mode', 'verify');
      
      fetch(`${func2url['steam-auth']}?${verifyParams.toString()}`)
        .then(res => res.json())
        .then(data => {
          if (data.steamId) {
            setUser(data);
            localStorage.setItem('steamUser', JSON.stringify(data));
            window.history.replaceState({}, '', window.location.pathname);
          }
        })
        .catch(err => console.error('Steam auth error:', err));
    }
  }, []);

  const loadNews = async () => {
    const cachedNews = localStorage.getItem('newsItems');
    if (cachedNews) {
      setNewsItems(JSON.parse(cachedNews));
    }

    try {
      const response = await fetch(func2url.news);
      const data = await response.json();
      const formattedNews = (data.news || []).map((item: any) => ({
        id: item.id,
        title: item.title,
        description: item.content.substring(0, 150) + '...',
        date: formatShortDate(item.date)
      }));
      setNewsItems(formattedNews);
      localStorage.setItem('newsItems', JSON.stringify(formattedNews));
    } catch (error) {
      console.error('Failed to load news:', error);
    }
  };

  const handleSteamLogin = async () => {
    const returnUrl = `${window.location.origin}${window.location.pathname}`;
    const response = await fetch(`${func2url['steam-auth']}?mode=login&return_url=${encodeURIComponent(returnUrl)}`);
    const data = await response.json();
    
    if (data.redirectUrl) {
      window.location.href = data.redirectUrl;
    }
  };

  const handleLogout = () => {
    setUser(null);
    localStorage.removeItem('steamUser');
  };



  return (
      <main className="container mx-auto px-6 py-16">
        <div className="space-y-10">
          <div className="space-y-3">
            <div className="inline-block px-4 py-1.5 bg-primary/10 border border-primary/20 rounded-full mb-2">
              <span className="text-sm font-medium text-primary">Последние обновления</span>
            </div>
            <h2 className="text-5xl font-bold tracking-tight bg-gradient-to-r from-foreground to-foreground/60 bg-clip-text text-transparent">
              Новости
            </h2>
            <p className="text-muted-foreground text-xl">Следите за событиями и обновлениями игры</p>
          </div>
          
          <div className="grid gap-6 md:grid-cols-2">
            {newsItems.map((item) => (
              <Card key={item.id} className="group p-6 backdrop-blur-sm bg-card/50 border-border hover:border-primary/50 transition-all duration-300 hover:shadow-xl hover:shadow-primary/10 hover:scale-[1.02]">
                <div className="flex items-start gap-4">
                  <div className="w-12 h-12 rounded-xl bg-primary/10 flex items-center justify-center flex-shrink-0 group-hover:bg-primary/20 transition-colors">
                    <Icon name="Newspaper" size={24} className="text-primary" />
                  </div>
                  <div className="space-y-3 flex-1">
                    <div className="space-y-1">
                      <h3 className="text-xl font-bold group-hover:text-primary transition-colors">{item.title}</h3>
                      <p className="text-sm text-muted-foreground flex items-center gap-2">
                        <Icon name="Calendar" size={14} />
                        {item.date}
                      </p>
                    </div>
                    <p className="text-muted-foreground leading-relaxed">{item.description}</p>
                  </div>
                </div>
              </Card>
            ))}
          </div>
        </div>
      </main>
  );
};

export default News;