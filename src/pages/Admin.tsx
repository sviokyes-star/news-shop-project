import { useState, useEffect } from 'react';
import func2url from '../../backend/func2url.json';
import NewsManagement from '@/components/admin/NewsManagement';
import ShopManagement from '@/components/admin/ShopManagement';
import ServersManagement from '@/components/admin/ServersManagement';
import UsersManagement from '@/components/admin/UsersManagement';
import TournamentsManagement from '@/components/admin/TournamentsManagement';
import PartnersManagement from '@/components/admin/PartnersManagement';
import MenuManagement from '@/components/admin/MenuManagement';
import ChatManagement from '@/components/admin/ChatManagement';
import SiteSettings from '@/components/admin/SiteSettings';
import AdminHeader from '@/components/admin/AdminHeader';
import AdminTabs from '@/components/admin/AdminTabs';
import AdminAccessDenied from '@/components/admin/AdminAccessDenied';
import AdminLoadingState from '@/components/admin/AdminLoadingState';

interface NewsItem {
  id: number;
  title: string;
  category: string;
  date: string;
  image_url?: string;
  content: string;
  badge?: string;
}

interface ShopItem {
  id: number;
  name: string;
  amount: string;
  price: number;
  is_active: boolean;
  order_position: number;
}

interface SteamUser {
  steamId: string;
  personaName: string;
  avatarUrl: string;
  profileUrl: string;
}

interface Server {
  id: number;
  name: string;
  ipAddress: string;
  port: number;
  gameType: string;
  map: string;
  maxPlayers: number;
  currentPlayers: number;
  status: string;
  description: string;
  isActive: boolean;
  orderPosition: number;
}

interface User {
  id: number;
  steamId: string;
  personaName: string;
  avatarUrl: string | null;
  profileUrl: string | null;
  balance: number;
  isBlocked: boolean;
  blockReason: string | null;
  isAdmin: boolean;
  isModerator: boolean;
  lastLogin: string | null;
  createdAt: string | null;
}

interface Tournament {
  id: number;
  name: string;
  description: string;
  prize_pool: number;
  max_participants: number;
  status: string;
  tournament_type: string;
  start_date: string;
  participants_count: number;
}

interface Partner {
  id: number;
  name: string;
  description: string;
  logo: string;
  website: string;
  category: string;
  isActive: boolean;
  orderPosition: number;
}

type TabType = 'news' | 'shop' | 'servers' | 'users' | 'tournaments' | 'partners' | 'menu' | 'chat' | 'settings';

export default function Admin() {
  const [activeTab, setActiveTab] = useState<TabType>('news');
  const [news, setNews] = useState<NewsItem[]>([]);
  const [shopItems, setShopItems] = useState<ShopItem[]>([]);
  const [servers, setServers] = useState<Server[]>([]);
  const [users, setUsers] = useState<User[]>([]);
  const [tournaments, setTournaments] = useState<Tournament[]>([]);
  const [partners, setPartners] = useState<Partner[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingShop, setIsLoadingShop] = useState(false);
  const [isLoadingServers, setIsLoadingServers] = useState(false);
  const [isLoadingUsers, setIsLoadingUsers] = useState(false);
  const [isLoadingTournaments, setIsLoadingTournaments] = useState(false);
  const [isLoadingPartners, setIsLoadingPartners] = useState(false);
  const [user, setUser] = useState<SteamUser | null>(null);
  const [isAdmin, setIsAdmin] = useState(false);
  const [isCheckingAccess, setIsCheckingAccess] = useState(true);
  const [logoUrl, setLogoUrl] = useState('https://cdn.poehali.dev/projects/0cd5ea72-8c09-43b2-b92c-a0fdee84371e/files/favicon-1771578220171.png');

  useEffect(() => {
    checkAccess();
  }, []);

  useEffect(() => {
    if (isAdmin) {
      loadNews();
      loadShopItems();
      loadServers();
      loadUsers();
      loadTournaments();
      loadPartners();
    }
  }, [isAdmin]);

  useEffect(() => {
    if (isAdmin && activeTab === 'servers') {
      updateServersStatus();
    }
  }, [isAdmin, activeTab]);

  const checkAccess = async () => {
    const savedUser = localStorage.getItem('steamUser');
    if (!savedUser) {
      setIsCheckingAccess(false);
      return;
    }

    const userData = JSON.parse(savedUser);
    setUser(userData);

    try {
      const response = await fetch(`${func2url['check-admin']}?steam_id=${userData.steamId}`);
      const data = await response.json();
      setIsAdmin(data.isAdmin);
    } catch (error) {
      console.error('Failed to check admin access:', error);
      setIsAdmin(false);
    } finally {
      setIsCheckingAccess(false);
    }
  };

  const handleSteamLogin = async () => {
    const returnUrl = `${window.location.origin}/admin`;
    const response = await fetch(`${func2url['steam-auth']}?mode=login&return_url=${encodeURIComponent(returnUrl)}`);
    const data = await response.json();
    
    if (data.redirectUrl) {
      window.location.href = data.redirectUrl;
    }
  };

  const loadNews = async () => {
    try {
      const response = await fetch(func2url.news);
      const data = await response.json();
      setNews(data.news || []);
    } catch (error) {
      console.error('Failed to load news:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const loadShopItems = async () => {
    setIsLoadingShop(true);
    try {
      const response = await fetch(`${func2url['shop-items']}?include_inactive=true`);
      const data = await response.json();
      setShopItems(data.items || []);
    } catch (error) {
      console.error('Failed to load shop items:', error);
    } finally {
      setIsLoadingShop(false);
    }
  };

  const loadServers = async () => {
    setIsLoadingServers(true);
    try {
      const response = await fetch(func2url.servers);
      const data = await response.json();
      setServers(data.servers || []);
    } catch (error) {
      console.error('Failed to load servers:', error);
    } finally {
      setIsLoadingServers(false);
    }
  };

  const loadUsers = async () => {
    setIsLoadingUsers(true);
    try {
      const response = await fetch(func2url.users);
      const data = await response.json();
      setUsers(data.users || []);
    } catch (error) {
      console.error('Failed to load users:', error);
    } finally {
      setIsLoadingUsers(false);
    }
  };

  const loadTournaments = async () => {
    setIsLoadingTournaments(true);
    try {
      const response = await fetch(func2url.tournaments);
      const data = await response.json();
      setTournaments(data.tournaments || []);
    } catch (error) {
      console.error('Failed to load tournaments:', error);
    } finally {
      setIsLoadingTournaments(false);
    }
  };

  const loadPartners = async () => {
    setIsLoadingPartners(true);
    try {
      const response = await fetch(`${func2url.partners}?include_inactive=true`);
      const data = await response.json();
      setPartners(data.partners || []);
    } catch (error) {
      console.error('Failed to load partners:', error);
    } finally {
      setIsLoadingPartners(false);
    }
  };

  const updateServersStatus = async () => {
    try {
      const response = await fetch(func2url['server-status'], {
        method: 'POST'
      });
      const data = await response.json();
      if (data.servers) {
        setServers(prevServers => {
          return prevServers.map(server => {
            const updatedServer = data.servers.find((s: any) => s.id === server.id);
            return updatedServer ? { ...server, ...updatedServer } : server;
          });
        });
      }
    } catch (error) {
      console.error('Failed to update server status:', error);
    }
  };

  if (isCheckingAccess) {
    return <AdminLoadingState />;
  }

  if (!user || !isAdmin) {
    return <AdminAccessDenied user={user} onSteamLogin={handleSteamLogin} />;
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-background via-background to-primary/5">
      <AdminHeader user={user} />
      <AdminTabs activeTab={activeTab} setActiveTab={setActiveTab} />

      <main className="container mx-auto px-6 py-8">
        {activeTab === 'news' && (
          <NewsManagement 
            news={news}
            isLoading={isLoading}
            onRefresh={loadNews}
          />
        )}

        {activeTab === 'shop' && (
          <ShopManagement
            shopItems={shopItems}
            isLoading={isLoadingShop}
            onRefresh={loadShopItems}
          />
        )}

        {activeTab === 'servers' && (
          <ServersManagement
            servers={servers}
            isLoading={isLoadingServers}
            onRefresh={loadServers}
            onUpdateStatus={updateServersStatus}
          />
        )}

        {activeTab === 'users' && (
          <UsersManagement
            users={users}
            isLoadingUsers={isLoadingUsers}
            adminUser={user}
            onReload={loadUsers}
          />
        )}

        {activeTab === 'tournaments' && user && (
          <TournamentsManagement
            tournaments={tournaments}
            user={user}
            onReload={loadTournaments}
          />
        )}

        {activeTab === 'partners' && (
          <PartnersManagement
            partners={partners}
            isLoading={isLoadingPartners}
            onRefresh={loadPartners}
          />
        )}

        {activeTab === 'menu' && (
          <MenuManagement />
        )}

        {activeTab === 'chat' && user && (
          <ChatManagement user={user} />
        )}

        {activeTab === 'settings' && (
          <SiteSettings
            currentLogoUrl={logoUrl}
            onLogoUpdated={(url) => {
              setLogoUrl(url);
              localStorage.setItem('site_logo_url', url);
            }}
          />
        )}
      </main>
    </div>
  );
}