import Icon from '@/components/ui/icon';

type TabType = 'news' | 'shop' | 'servers' | 'users' | 'tournaments' | 'partners' | 'menu' | 'chat' | 'settings';

interface AdminTabsProps {
  activeTab: TabType;
  setActiveTab: (tab: TabType) => void;
}

export default function AdminTabs({ activeTab, setActiveTab }: AdminTabsProps) {
  const tabs: { id: TabType; label: string; icon: string }[] = [
    { id: 'news', label: 'Новости', icon: 'Newspaper' },
    { id: 'shop', label: 'Магазин', icon: 'ShoppingBag' },
    { id: 'servers', label: 'Сервера', icon: 'Server' },
    { id: 'users', label: 'Пользователи', icon: 'Users' },
    { id: 'tournaments', label: 'Турниры', icon: 'Trophy' },
    { id: 'partners', label: 'Партнёры', icon: 'Handshake' },
    { id: 'menu', label: 'Меню', icon: 'Menu' },
    { id: 'chat', label: 'Чат', icon: 'MessageCircle' },
    { id: 'settings', label: 'Настройки', icon: 'Settings' },
  ];

  return (
    <div className="border-b border-border bg-background/80 backdrop-blur-sm sticky top-[73px] z-40">
      <div className="container mx-auto px-6">
        <div className="flex gap-2 overflow-x-auto scrollbar-hide">
          {tabs.map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={`flex items-center gap-2 px-6 py-4 font-medium transition-all whitespace-nowrap ${
                activeTab === tab.id
                  ? 'text-primary border-b-2 border-primary'
                  : 'text-muted-foreground hover:text-foreground'
              }`}
            >
              <Icon name={tab.icon as any} size={18} />
              {tab.label}
            </button>
          ))}
        </div>
      </div>
    </div>
  );
}