import { useState, useRef } from 'react';
import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import Icon from '@/components/ui/icon';
import { toast } from '@/hooks/use-toast';
import func2url from '../../../backend/func2url.json';
import MenuManagement from './MenuManagement';

interface SiteSettingsProps {
  currentLogoUrl: string;
  onLogoUpdated: (url: string) => void;
}

export default function SiteSettings({ currentLogoUrl, onLogoUpdated }: SiteSettingsProps) {
  const [isUploading, setIsUploading] = useState(false);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    if (file.size > 5 * 1024 * 1024) {
      toast({ title: 'Ошибка', description: 'Максимальный размер файла — 5MB', variant: 'destructive' });
      return;
    }
    if (!file.type.startsWith('image/')) {
      toast({ title: 'Ошибка', description: 'Только изображения', variant: 'destructive' });
      return;
    }

    const reader = new FileReader();
    reader.onloadend = () => setPreviewUrl(reader.result as string);
    reader.readAsDataURL(file);
  };

  const handleUpload = async () => {
    if (!previewUrl) return;
    setIsUploading(true);
    try {
      const res = await fetch(func2url['site-settings'], {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ image: previewUrl }),
      });
      const data = await res.json();
      if (data.success) {
        onLogoUpdated(data.logo_url);
        setPreviewUrl(null);
        toast({ title: 'Логотип обновлён!' });
      } else {
        toast({ title: 'Ошибка', description: data.error, variant: 'destructive' });
      }
    } catch {
      toast({ title: 'Ошибка загрузки', variant: 'destructive' });
    }
    setIsUploading(false);
  };

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold">Настройки сайта</h2>
        <p className="text-muted-foreground mt-1">Управление логотипом и внешним видом</p>
      </div>

      <Card className="p-6 space-y-6">
        <h3 className="text-lg font-semibold flex items-center gap-2">
          <Icon name="Image" size={20} className="text-primary" />
          Логотип сайта
        </h3>

        <div className="flex items-center gap-8">
          <div className="space-y-2">
            <p className="text-sm text-muted-foreground">Текущий логотип</p>
            <img src={currentLogoUrl} alt="Логотип" className="w-20 h-20 rounded-xl object-contain border border-border bg-card" />
          </div>

          {previewUrl && (
            <div className="space-y-2">
              <p className="text-sm text-muted-foreground">Новый логотип</p>
              <img src={previewUrl} alt="Превью" className="w-20 h-20 rounded-xl object-contain border border-primary bg-card" />
            </div>
          )}
        </div>

        <div className="flex items-center gap-3">
          <input
            ref={fileInputRef}
            type="file"
            accept="image/*"
            onChange={handleFileChange}
            className="hidden"
          />
          <Button variant="outline" onClick={() => fileInputRef.current?.click()} className="gap-2">
            <Icon name="Upload" size={16} />
            Выбрать изображение
          </Button>
          {previewUrl && (
            <>
              <Button onClick={handleUpload} disabled={isUploading} className="gap-2">
                {isUploading ? <Icon name="Loader2" size={16} className="animate-spin" /> : <Icon name="Check" size={16} />}
                {isUploading ? 'Загружаю...' : 'Сохранить'}
              </Button>
              <Button variant="ghost" onClick={() => setPreviewUrl(null)}>
                Отмена
              </Button>
            </>
          )}
        </div>
        <p className="text-xs text-muted-foreground">PNG, JPG, SVG до 5MB. Рекомендуется квадратное изображение.</p>
      </Card>

      <MenuManagement />
    </div>
  );
}