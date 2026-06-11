import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import Icon from '@/components/ui/icon';
import { useState, useEffect } from 'react';
import func2url from '../../backend/func2url.json';

interface Server {
  id: number;
  name: string;
  ipAddress: string;
  port: number;
  gameType: string;
  map: string;
  currentPlayers: number;
  maxPlayers: number;
  status: 'online' | 'offline' | 'maintenance';
}

const GAME_ICONS: Record<string, string> = {
  'Counter-Strike 2': 'https://cdn.poehali.dev/files/0ec16141-74ad-4adf-8da0-81c446eced42.png',
  'Counter-Strike: Source': 'https://upload.wikimedia.org/wikipedia/ru/d/d7/Counter-Strike_Source_cover.jpg',
};

const ServersTab = () => {
  const [servers, setServers] = useState<Server[]>([]);

  const loadServers = async () => {
    const cachedServers = localStorage.getItem('servers');
    if (cachedServers) setServers(JSON.parse(cachedServers));
    try {
      const response = await fetch(func2url.servers);
      const data = await response.json();
      setServers(data.servers || []);
      localStorage.setItem('servers', JSON.stringify(data.servers || []));
    } catch (error) {
      console.error('Failed to load servers:', error);
    }
  };

  const updateServersStatus = async () => {
    try {
      const response = await fetch(`${func2url.servers}?action=status`, { method: 'POST' });
      const data = await response.json();
      if (data.servers) {
        setServers(prev => prev.map(server => {
          const updated = data.servers.find((s: Server) => s.id === server.id);
          return updated ? { ...server, ...updated } : server;
        }));
      }
    } catch (error) {
      console.error('Failed to update server status:', error);
    }
  };

  useEffect(() => {
    loadServers();
    updateServersStatus();
  }, []);

  const copyToClipboard = (text: string) => {
    navigator.clipboard.writeText(text);
  };

  const grouped = servers.reduce<Record<string, Server[]>>((acc, server) => {
    const key = server.gameType || 'Другое';
    if (!acc[key]) acc[key] = [];
    acc[key].push(server);
    return acc;
  }, {});

  return (
    <div className="space-y-10">
      <div className="space-y-3">
        <div className="inline-block px-4 py-1.5 bg-primary/10 border border-primary/20 rounded-full mb-2">
          <span className="text-sm font-medium text-primary">Игровые сервера</span>
        </div>
        <p className="text-muted-foreground text-xl">Выберите сервер и присоединяйтесь к игре</p>
      </div>

      <div className="space-y-10">
        {Object.entries(grouped).map(([gameType, gameServers]) => (
          <div key={gameType} className="space-y-4">
            <div className="flex items-center gap-3">
              {GAME_ICONS[gameType] ? (
                <img src={GAME_ICONS[gameType]} alt={gameType} className="w-8 h-8 rounded object-cover" />
              ) : (
                <Icon name="Gamepad2" size={28} className="text-primary" />
              )}
              <h2 className="text-2xl font-bold">{gameType}</h2>
              <span className="text-sm text-muted-foreground">
                {gameServers.filter(s => s.status === 'online').length}/{gameServers.length} онлайн
              </span>
            </div>

            <div className="grid gap-4">
              {gameServers.map((server, index) => (
                <Card
                  key={server.id}
                  className="group p-8 border border-border hover:border-primary/50 transition-all duration-300 hover:shadow-2xl hover:shadow-primary/10 bg-card/50 backdrop-blur"
                  style={{ animationDelay: `${index * 100}ms` }}
                >
                  <div className="flex items-center justify-between gap-6">
                    <div className="flex items-center gap-6 flex-1">
                      <div className="w-16 h-16 rounded-full bg-primary/20 flex items-center justify-center group-hover:bg-primary group-hover:text-primary-foreground transition-all duration-300">
                        <Icon name="Server" size={28} />
                      </div>

                      <div className="space-y-2 flex-1">
                        <div className="flex items-center gap-3">
                          <h3 className="text-2xl font-bold tracking-tight">{server.name}</h3>
                          <span className={`flex items-center gap-1.5 px-3 py-1 rounded-full text-sm font-medium border ${
                            server.status === 'online' ? 'bg-green-500/10 text-green-500 border-green-500/20' :
                            server.status === 'offline' ? 'bg-red-500/10 text-red-500 border-red-500/20' :
                            'bg-yellow-500/10 text-yellow-500 border-yellow-500/20'
                          }`}>
                            <div className={`w-2 h-2 rounded-full ${
                              server.status === 'online' ? 'bg-green-500 animate-pulse' :
                              server.status === 'offline' ? 'bg-red-500' :
                              'bg-yellow-500'
                            }`} />
                            {server.status === 'online' ? 'Онлайн' : server.status === 'offline' ? 'Оффлайн' : 'Тех. работы'}
                          </span>
                        </div>

                        <div className="flex items-center gap-6 text-sm text-muted-foreground">
                          <div className="flex items-center gap-2">
                            <Icon name="Map" size={16} />
                            <span>{server.map || 'Загрузка...'}</span>
                          </div>
                          <div className="flex items-center gap-2">
                            <Icon name="Users" size={16} />
                            <span>{server.currentPlayers}/{server.maxPlayers}</span>
                          </div>
                          <div className="flex items-center gap-2">
                            <Icon name="Globe" size={16} />
                            <span>{server.ipAddress}:{server.port}</span>
                          </div>
                        </div>
                      </div>
                    </div>

                    <div className="flex gap-3">
                      <Button
                        variant="outline"
                        size="lg"
                        onClick={() => copyToClipboard(`${server.ipAddress}:${server.port}`)}
                        className="gap-2"
                      >
                        <Icon name="Copy" size={18} />
                        IP
                      </Button>
                      <Button
                        size="lg"
                        className="gap-2 shadow-lg shadow-primary/20"
                        onClick={() => window.location.href = `steam://connect/${server.ipAddress}:${server.port}`}
                      >
                        <Icon name="Play" size={18} />
                        Подключиться
                      </Button>
                    </div>
                  </div>
                </Card>
              ))}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

export default ServersTab;
