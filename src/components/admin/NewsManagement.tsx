import { useState, useEffect } from 'react';
import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import Icon from '@/components/ui/icon';
import func2url from '../../../backend/func2url.json';
import { toLocalDateTimeInput, toUTCISOString } from '@/utils/dateFormat';

interface NewsItem {
  id: number;
  title: string;
  category: string;
  date: string;
  image_url?: string;
  content: string;
  badge?: string;
}

interface Comment {
  id: number;
  news_id: number;
  author: string;
  text: string;
  avatar: string;
  steam_id: string | null;
  avatar_url: string | null;
  date: string;
}

interface SteamUser {
  steamId: string;
  personaName: string;
  avatarUrl: string;
  profileUrl: string;
}

interface NewsManagementProps {
  news: NewsItem[];
  isLoading: boolean;
  onRefresh: () => Promise<void>;
}

export default function NewsManagement({ news, isLoading, onRefresh }: NewsManagementProps) {
  const [user, setUser] = useState<SteamUser | null>(null);

  useEffect(() => {
    const savedUser = localStorage.getItem('steamUser');
    if (savedUser) {
      setUser(JSON.parse(savedUser));
    }
  }, []);
  const [comments, setComments] = useState<Comment[]>([]);
  const [isLoadingComments, setIsLoadingComments] = useState(false);
  const [selectedNewsId, setSelectedNewsId] = useState<number | null>(null);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [error, setError] = useState<string>('');
  const [success, setSuccess] = useState<string>('');
  const [formData, setFormData] = useState({
    title: '',
    category: '',
    image_url: '',
    content: '',
    badge: '',
    date: toLocalDateTimeInput(new Date().toISOString())
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    console.log('🔵 handleSubmit called', { user, editingId, formData });
    
    if (!user) {
      console.log('❌ No user');
      setError('Пользователь не авторизован');
      return;
    }
    
    setError('');
    setSuccess('');
    
    try {
      const url = func2url.news;
      const method = editingId ? 'PUT' : 'POST';
      const bodyData = { ...formData, date: toUTCISOString(formData.date) };
      const body = editingId 
        ? { ...bodyData, id: editingId }
        : bodyData;

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
        setSuccess(editingId ? 'Новость обновлена' : 'Новость добавлена');
        await onRefresh();
        setTimeout(() => {
          resetForm();
        }, 1000);
      } else {
        const data = await response.json();
        console.log('❌ Error response:', data);
        setError(data.error || 'Ошибка при сохранении');
      }
    } catch (error) {
      console.error('❌ Failed to save news:', error);
      setError('Ошибка соединения с сервером');
    }
  };

  const handleEdit = (item: NewsItem) => {
    setEditingId(item.id);
    setFormData({
      title: item.title,
      category: item.category,
      image_url: item.image_url || '',
      content: item.content,
      badge: item.badge || '',
      date: toLocalDateTimeInput(item.date)
    });
  };

  const handleDelete = async (id: number) => {
    if (!confirm('Удалить эту новость?')) return;
    
    if (!user) {
      setError('Пользователь не авторизован');
      return;
    }

    try {
      const response = await fetch(`${func2url.news}?id=${id}`, {
        method: 'DELETE',
        headers: {
          'x-admin-steam-id': user.steamId
        }
      });

      if (response.ok) {
        setSuccess('Новость удалена');
        await onRefresh();
        setTimeout(() => setSuccess(''), 2000);
      } else {
        const data = await response.json();
        setError(data.error || 'Ошибка при удалении');
      }
    } catch (error) {
      console.error('Failed to delete news:', error);
      setError('Ошибка соединения с сервером');
    }
  };

  const resetForm = () => {
    setEditingId(null);
    setFormData({
      title: '',
      category: '',
      image_url: '',
      content: '',
      badge: '',
      date: toLocalDateTimeInput(new Date().toISOString())
    });
    setError('');
    setSuccess('');
  };

  const loadComments = async (newsId: number) => {
    setIsLoadingComments(true);
    setSelectedNewsId(newsId);
    
    try {
      const response = await fetch(`${func2url.comments}?news_id=${newsId}`);
      const data = await response.json();
      setComments(data.comments || []);
    } catch (error) {
      console.error('Failed to load comments:', error);
      setComments([]);
    } finally {
      setIsLoadingComments(false);
    }
  };

  const handleDeleteComment = async (commentId: number) => {
    if (!confirm('Удалить этот комментарий?')) return;
    if (!user) return;

    try {
      const response = await fetch(
        `${func2url.comments}?id=${commentId}&admin_steam_id=${user.steamId}`,
        { method: 'DELETE' }
      );

      if (response.ok && selectedNewsId) {
        await loadComments(selectedNewsId);
      }
    } catch (error) {
      console.error('Failed to delete comment:', error);
    }
  };

  return (
    <div className="grid lg:grid-cols-2 gap-6">
      <div>
        <Card className="p-6 bg-card/80 backdrop-blur border-primary/20">
          <h2 className="text-2xl font-bold mb-6 flex items-center gap-2">
            <Icon name={editingId ? "Edit" : "Plus"} size={24} />
            {editingId ? 'Редактировать новость' : 'Добавить новость'}
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
          
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-sm font-medium mb-2">Заголовок</label>
              <Input
                value={formData.title}
                onChange={(e) => setFormData({ ...formData, title: e.target.value })}
                placeholder="Название новости"
                required
              />
            </div>

            <div>
              <label className="block text-sm font-medium mb-2">Категория</label>
              <Input
                value={formData.category}
                onChange={(e) => setFormData({ ...formData, category: e.target.value })}
                placeholder="Обновления, События, и т.д."
                required
              />
            </div>

            <div>
              <label className="block text-sm font-medium mb-2">URL изображения</label>
              <Input
                value={formData.image_url}
                onChange={(e) => setFormData({ ...formData, image_url: e.target.value })}
                placeholder="https://..."
              />
            </div>

            <div>
              <label className="block text-sm font-medium mb-2">Бейдж (опционально)</label>
              <Input
                value={formData.badge}
                onChange={(e) => setFormData({ ...formData, badge: e.target.value })}
                placeholder="НОВОЕ, ВАЖНО, и т.д."
              />
            </div>

            <div>
              <label className="block text-sm font-medium mb-2">Дата публикации</label>
              <Input
                type="datetime-local"
                value={formData.date}
                onChange={(e) => setFormData({ ...formData, date: e.target.value })}
                required
              />
            </div>

            <div>
              <label className="block text-sm font-medium mb-2">Содержание</label>
              <Textarea
                value={formData.content}
                onChange={(e) => setFormData({ ...formData, content: e.target.value })}
                placeholder="Текст новости..."
                rows={6}
                required
              />
            </div>

            <div className="flex gap-3">
              <Button type="submit" className="flex-1">
                <Icon name={editingId ? "Save" : "Plus"} size={18} className="mr-2" />
                {editingId ? 'Сохранить' : 'Добавить'}
              </Button>
              {editingId && (
                <Button type="button" variant="outline" onClick={resetForm}>
                  Отмена
                </Button>
              )}
            </div>
          </form>
        </Card>
      </div>

      <div>
        <Card className="p-6 bg-card/80 backdrop-blur border-primary/20">
          <h2 className="text-2xl font-bold mb-6 flex items-center gap-2">
            <Icon name="Newspaper" size={24} />
            Список новостей ({news.length})
          </h2>
          
          {isLoading ? (
            <div className="text-center py-12 text-muted-foreground">
              Загрузка новостей...
            </div>
          ) : news.length === 0 ? (
            <div className="text-center py-12 text-muted-foreground">
              <Icon name="Newspaper" size={48} className="mx-auto mb-3 opacity-20" />
              <p>Новости не добавлены</p>
            </div>
          ) : (
            <div className="space-y-3 max-h-[600px] overflow-y-auto">
              {news.map((item) => (
                <div
                  key={item.id}
                  className="p-4 rounded-lg border border-border bg-background/50 hover:border-primary/30 transition-colors"
                >
                  <div className="flex items-start justify-between gap-3 mb-2">
                    <div className="flex-1 min-w-0">
                      <h3 className="font-semibold mb-1 truncate">{item.title}</h3>
                      <div className="flex items-center gap-2 text-xs text-muted-foreground">
                        <span className="px-2 py-0.5 bg-primary/10 rounded">{item.category}</span>
                        {item.badge && (
                          <span className="px-2 py-0.5 bg-yellow-500/10 text-yellow-500 rounded">{item.badge}</span>
                        )}
                        <span>{new Date(item.date).toLocaleDateString('ru-RU', { timeZone: 'Europe/Moscow' })}</span>
                      </div>
                    </div>
                  </div>

                  <p className="text-sm text-muted-foreground mb-3 line-clamp-2">{item.content}</p>

                  <div className="flex gap-2">
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => handleEdit(item)}
                      className="flex-1"
                    >
                      <Icon name="Edit" size={14} className="mr-1" />
                      Редактировать
                    </Button>
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => loadComments(item.id)}
                      className="flex-1"
                    >
                      <Icon name="MessageSquare" size={14} className="mr-1" />
                      Комментарии
                    </Button>
                    <Button
                      size="sm"
                      variant="destructive"
                      onClick={() => handleDelete(item.id)}
                    >
                      <Icon name="Trash2" size={14} />
                    </Button>
                  </div>

                  {selectedNewsId === item.id && (
                    <div className="mt-4 pt-4 border-t border-border">
                      <h4 className="font-medium mb-3 flex items-center gap-2">
                        <Icon name="MessageSquare" size={16} />
                        Комментарии ({comments.length})
                      </h4>
                      
                      {isLoadingComments ? (
                        <div className="text-center py-6 text-sm text-muted-foreground">
                          Загрузка комментариев...
                        </div>
                      ) : comments.length === 0 ? (
                        <div className="text-center py-6 text-sm text-muted-foreground">
                          Нет комментариев
                        </div>
                      ) : (
                        <div className="space-y-2 max-h-60 overflow-y-auto">
                          {comments.map((comment) => (
                            <div
                              key={comment.id}
                              className="p-3 rounded bg-background border border-border/50"
                            >
                              <div className="flex items-start justify-between gap-2 mb-2">
                                <div className="flex items-center gap-2">
                                  {comment.avatar_url ? (
                                    <img
                                      src={comment.avatar_url}
                                      alt={comment.author}
                                      className="w-6 h-6 rounded-full"
                                    />
                                  ) : (
                                    <div className="w-6 h-6 rounded-full bg-primary/20 flex items-center justify-center">
                                      <Icon name="User" size={14} />
                                    </div>
                                  )}
                                  <span className="text-sm font-medium">{comment.author}</span>
                                </div>
                                <Button
                                  size="sm"
                                  variant="ghost"
                                  onClick={() => handleDeleteComment(comment.id)}
                                  className="h-6 w-6 p-0"
                                >
                                  <Icon name="Trash2" size={12} />
                                </Button>
                              </div>
                              <p className="text-sm text-muted-foreground">{comment.text}</p>
                              <span className="text-xs text-muted-foreground/60 mt-1 block">
                                {new Date(comment.date).toLocaleString()}
                              </span>
                            </div>
                          ))}
                        </div>
                      )}
                    </div>
                  )}
                </div>
              ))}
            </div>
          )}
        </Card>
      </div>
    </div>
  );
}