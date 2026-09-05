import { useState, useEffect } from 'react';
import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import Icon from '@/components/ui/icon';
import func2url from '@/lib/api';

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

interface SteamUser {
  steamId: string;
  personaName: string;
  avatarUrl: string;
  profileUrl: string;
}

interface ServersManagementProps {
  servers: Server[];
  isLoading: boolean;
  onRefresh: () => Promise<void>;
  onUpdateStatus: () => Promise<void>;
}

export default function ServersManagement({ 
  servers, 
  isLoading, 
  onRefresh,
  onUpdateStatus 
}: ServersManagementProps) {
  const [user, setUser] = useState<SteamUser | null>(null);

  useEffect(() => {
    const savedUser = localStorage.getItem('steamUser');
    if (savedUser) {
      setUser(JSON.parse(savedUser));
    }
  }, []);
  const [editingServerId, setEditingServerId] = useState<number | null>(null);
  const [error, setError] = useState<string>('');
  const [success, setSuccess] = useState<string>('');
  const [serverFormData, setServerFormData] = useState({
    name: '',
    ipAddress: '',
    port: 27015,
    gameType: 'Counter-Strike: Source',
    map: '',
    maxPlayers: 32,
    currentPlayers: 0,
    status: 'online',
    description: ''
  });

  const handleServerSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    console.log('🔵 handleServerSubmit called', { user, editingServerId, serverFormData });
    
    if (!user) {
      console.log('❌ No user');
      setError('Пользователь не авторизован');
      return;
    }
    
    setError('');
    setSuccess('');

    try {
      const url = func2url.servers;
      const method = editingServerId ? 'PUT' : 'POST';
      const body = editingServerId 
        ? { ...serverFormData, id: editingServerId }
        : serverFormData;

      console.log('📤 Sending request:', { url, method, body });

      const response = await fetch(url, {
        method,
        headers: {
          'Content-Type': 'application/json',
          'X-Admin-Steam-Id': user.steamId
        },
        body: JSON.stringify(body)
      });

      console.log('📥 Response:', response.status, response.ok);

      if (response.ok) {
        setSuccess(editingServerId ? 'Сервер обновлён' : 'Сервер добавлен');
        await onRefresh();
        setTimeout(() => {
          resetServerForm();
        }, 1000);
      } else {
        const data = await response.json();
        console.log('❌ Error response:', data);
        setError(data.error || 'Ошибка при сохранении');
      }
    } catch (error) {
      console.error('❌ Failed to save server:', error);
      setError('Ошибка соединения с сервером');
    }
  };

  const handleEditServer = (server: Server) => {
    setEditingServerId(server.id);
    setServerFormData({
      name: server.name,
      ipAddress: server.ipAddress,
      port: server.port,
      gameType: server.gameType,
      map: server.map,
      maxPlayers: server.maxPlayers,
      currentPlayers: server.currentPlayers,
      status: server.status,
      description: server.description
    });
  };

  const handleDeleteServer = async (id: number) => {
    if (!confirm('Удалить этот сервер?')) return;
    if (!user) return;

    try {
      const response = await fetch(
        `${func2url.servers}?id=${id}`,
        {
          method: 'DELETE',
          headers: {
            'X-Admin-Steam-Id': user.steamId
          }
        }
      );

      if (response.ok) {
        await onRefresh();
      }
    } catch (error) {
      console.error('Failed to delete server:', error);
    }
  };

  const resetServerForm = () => {
    setEditingServerId(null);
    setServerFormData({
      name: '',
      ipAddress: '',
      port: 27015,
      gameType: 'Counter-Strike: Source',
      map: '',
      maxPlayers: 32,
      currentPlayers: 0,
      status: 'online',
      description: ''
    });
    setError('');
    setSuccess('');
  };

  const handleMoveServer = async (server: Server, direction: 'up' | 'down') => {
    if (!user) return;

    const sortedServers = [...servers].sort((a, b) => a.orderPosition - b.orderPosition);
    const currentIndex = sortedServers.findIndex(s => s.id === server.id);
    
    if (
      (direction === 'up' && currentIndex === 0) ||
      (direction === 'down' && currentIndex === sortedServers.length - 1)
    ) {
      return;
    }

    const swapIndex = direction === 'up' ? currentIndex - 1 : currentIndex + 1;
    const swapServer = sortedServers[swapIndex];

    try {
      await fetch(func2url.servers, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'X-Admin-Steam-Id': user.steamId
        },
        body: JSON.stringify({
          id: server.id,
          orderPosition: swapServer.orderPosition
        })
      });

      await fetch(func2url.servers, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'X-Admin-Steam-Id': user.steamId
        },
        body: JSON.stringify({
          id: swapServer.id,
          orderPosition: server.orderPosition
        })
      });

      await onReload();
    } catch (error) {
      console.error('Failed to reorder servers:', error);
    }
  };

  return (
    <div className="grid lg:grid-cols-2 gap-6">
      <div>
        <Card className="p-6 bg-card/80 backdrop-blur border-primary/20">
          <h2 className="text-2xl font-bold mb-6 flex items-center gap-2">
            <Icon name={editingServerId ? "Edit" : "Plus"} size={24} />
            {editingServerId ? 'Редактировать сервер' : 'Добавить сервер'}
          </h2>
          
          {error && (
            <div className="mb-4 p-3 bg-destructive/10 border border-destructive/20 rounded-lg text-destructive text-sm">
              {error}
            </div>
          )}
          {success && (
            <div className="mb-4 p-3 bg-green-500/10 border border-green-500/20 rounded-lg text-green-500 text-sm">
              {success}
            </div>
          )}
          
          <form onSubmit={handleServerSubmit} className="space-y-4">
            <div>
              <label className="block text-sm font-medium mb-2">Название сервера</label>
              <Input
                value={serverFormData.name}
                onChange={(e) => setServerFormData({ ...serverFormData, name: e.target.value })}
                placeholder="РУ Европа #1"
                required
              />
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-sm font-medium mb-2">IP адрес</label>
                <Input
                  value={serverFormData.ipAddress}
                  onChange={(e) => setServerFormData({ ...serverFormData, ipAddress: e.target.value })}
                  placeholder="192.168.1.1"
                  required
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-2">Порт</label>
                <Input
                  type="number"
                  value={serverFormData.port}
                  onChange={(e) => setServerFormData({ ...serverFormData, port: Number(e.target.value) })}
                  placeholder="27015"
                  required
                  min="1"
                  max="65535"
                />
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium mb-2">Тип игры</label>
              <Input
                value={serverFormData.gameType}
                onChange={(e) => setServerFormData({ ...serverFormData, gameType: e.target.value })}
                placeholder="Counter-Strike: Source"
                required
              />
            </div>

            <div>
              <label className="block text-sm font-medium mb-2">Карта (опционально)</label>
              <Input
                value={serverFormData.map}
                onChange={(e) => setServerFormData({ ...serverFormData, map: e.target.value })}
                placeholder="de_dust2"
              />
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-sm font-medium mb-2">Макс. игроков</label>
                <Input
                  type="number"
                  value={serverFormData.maxPlayers}
                  onChange={(e) => setServerFormData({ ...serverFormData, maxPlayers: Number(e.target.value) })}
                  placeholder="32"
                  required
                  min="1"
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-2">Статус</label>
                <select
                  value={serverFormData.status}
                  onChange={(e) => setServerFormData({ ...serverFormData, status: e.target.value })}
                  className="w-full px-3 py-2 rounded-md border border-input bg-background"
                  required
                >
                  <option value="online">Онлайн</option>
                  <option value="offline">Оффлайн</option>
                  <option value="maintenance">Тех. работы</option>
                </select>
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium mb-2">Описание (опционально)</label>
              <Textarea
                value={serverFormData.description}
                onChange={(e) => setServerFormData({ ...serverFormData, description: e.target.value })}
                placeholder="Описание сервера..."
                rows={3}
              />
            </div>

            <div className="flex gap-3">
              <Button type="submit" className="flex-1">
                <Icon name={editingServerId ? "Save" : "Plus"} size={18} className="mr-2" />
                {editingServerId ? 'Сохранить' : 'Добавить'}
              </Button>
              {editingServerId && (
                <Button type="button" variant="outline" onClick={resetServerForm}>
                  Отмена
                </Button>
              )}
            </div>
          </form>
        </Card>
      </div>

      <div>
        <Card className="p-6 bg-card/80 backdrop-blur border-primary/20">
          <div className="flex items-center justify-between mb-6">
            <h2 className="text-2xl font-bold flex items-center gap-2">
              <Icon name="Server" size={24} />
              Список серверов ({servers.length})
            </h2>
            <Button
              size="sm"
              variant="outline"
              onClick={onUpdateStatus}
              className="gap-2"
            >
              <Icon name="RefreshCw" size={16} />
              Обновить статусы
            </Button>
          </div>
          
          {isLoading ? (
            <div className="text-center py-12 text-muted-foreground">
              Загрузка серверов...
            </div>
          ) : servers.length === 0 ? (
            <div className="text-center py-12 text-muted-foreground">
              <Icon name="Server" size={48} className="mx-auto mb-3 opacity-20" />
              <p>Серверы не добавлены</p>
            </div>
          ) : (
            <div className="space-y-3 max-h-[600px] overflow-y-auto">
              {servers.map((server) => (
                <div
                  key={server.id}
                  className="p-4 rounded-lg border border-border bg-background/50 hover:border-primary/30 transition-colors"
                >
                  <div className="flex items-start justify-between gap-3 mb-3">
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 mb-1">
                        <h3 className="font-semibold truncate">{server.name}</h3>
                        <span className={`text-xs px-2 py-0.5 rounded ${
                          server.status === 'online' ? 'bg-green-500/20 text-green-500' :
                          server.status === 'offline' ? 'bg-red-500/20 text-red-500' :
                          'bg-yellow-500/20 text-yellow-500'
                        }`}>
                          {server.status === 'online' ? 'Онлайн' : 
                           server.status === 'offline' ? 'Оффлайн' : 'Тех. работы'}
                        </span>
                      </div>
                      <div className="flex flex-col gap-1 text-sm text-muted-foreground">
                        <div className="flex items-center gap-2">
                          <Icon name="Globe" size={14} />
                          <span className="font-mono text-xs">{server.ipAddress}:{server.port}</span>
                        </div>
                        <div className="flex items-center gap-3">
                          <div className="flex items-center gap-1">
                            <Icon name="Users" size={14} />
                            <span>{server.currentPlayers}/{server.maxPlayers}</span>
                          </div>
                          {server.map && (
                            <div className="flex items-center gap-1">
                              <Icon name="Map" size={14} />
                              <span>{server.map}</span>
                            </div>
                          )}
                        </div>
                        <div className="text-xs text-muted-foreground/70">
                          {server.gameType}
                        </div>
                      </div>
                    </div>
                  </div>

                  {server.description && (
                    <p className="text-sm text-muted-foreground mb-3 line-clamp-2">
                      {server.description}
                    </p>
                  )}

                  <div className="space-y-2">
                    <div className="flex gap-2">
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => handleMoveServer(server, 'up')}
                        disabled={servers.sort((a, b) => a.orderPosition - b.orderPosition).findIndex(s => s.id === server.id) === 0}
                        title="Переместить вверх"
                      >
                        <Icon name="ArrowUp" size={14} />
                      </Button>
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => handleMoveServer(server, 'down')}
                        disabled={servers.sort((a, b) => a.orderPosition - b.orderPosition).findIndex(s => s.id === server.id) === servers.length - 1}
                        title="Переместить вниз"
                      >
                        <Icon name="ArrowDown" size={14} />
                      </Button>
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => handleEditServer(server)}
                        className="flex-1"
                      >
                        <Icon name="Edit" size={14} className="mr-1" />
                        Редактировать
                      </Button>
                    </div>
                    <div className="flex gap-2">
                      <Button
                        size="sm"
                        variant="destructive"
                        onClick={() => handleDeleteServer(server.id)}
                        className="flex-1"
                      >
                        <Icon name="Trash2" size={14} />
                      </Button>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </Card>
      </div>
    </div>
  );
}