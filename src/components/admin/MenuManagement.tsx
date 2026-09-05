import { useState, useEffect } from 'react';
import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Switch } from '@/components/ui/switch';
import Icon from '@/components/ui/icon';
import func2url from '@/lib/api';

interface MenuItem {
  id: number;
  name: string;
  label: string;
  route: string;
  icon: string;
  isVisible: boolean;
  orderPosition: number;
}

const MenuManagement = () => {
  const [menuItems, setMenuItems] = useState<MenuItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    loadMenuItems();
  }, []);

  const loadMenuItems = async () => {
    try {
      const response = await fetch(func2url['menu-items']);
      const data = await response.json();
      setMenuItems(data.menuItems || []);
    } catch (error) {
      console.error('Failed to load menu items:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const toggleVisibility = (id: number) => {
    setMenuItems(prev =>
      prev.map(item =>
        item.id === id ? { ...item, isVisible: !item.isVisible } : item
      )
    );
  };

  const moveUp = (index: number) => {
    if (index === 0) return;
    
    const newItems = [...menuItems];
    const temp = newItems[index];
    newItems[index] = newItems[index - 1];
    newItems[index - 1] = temp;
    
    newItems.forEach((item, i) => {
      item.orderPosition = i + 1;
    });
    
    setMenuItems(newItems);
  };

  const moveDown = (index: number) => {
    if (index === menuItems.length - 1) return;
    
    const newItems = [...menuItems];
    const temp = newItems[index];
    newItems[index] = newItems[index + 1];
    newItems[index + 1] = temp;
    
    newItems.forEach((item, i) => {
      item.orderPosition = i + 1;
    });
    
    setMenuItems(newItems);
  };

  const saveChanges = async () => {
    setIsSaving(true);
    try {
      const response = await fetch(func2url['menu-items'], {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ menuItems })
      });

      const data = await response.json();
      
      if (response.ok) {
        alert('Изменения сохранены!');
      } else {
        alert(data.error || 'Ошибка сохранения');
      }
    } catch (error) {
      console.error('Failed to save menu items:', error);
      alert('Ошибка при сохранении');
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) {
    return (
      <div className="text-center py-12">
        <Icon name="Loader2" size={48} className="mx-auto mb-3 animate-spin text-primary" />
        <p className="text-muted-foreground">Загрузка...</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold">Управление меню</h2>
          <p className="text-muted-foreground">Настройте видимость и порядок пунктов меню</p>
        </div>
        <Button onClick={saveChanges} disabled={isSaving} className="gap-2">
          <Icon name="Save" size={18} />
          {isSaving ? 'Сохранение...' : 'Сохранить изменения'}
        </Button>
      </div>

      <div className="space-y-3">
        {menuItems.map((item, index) => (
          <Card key={item.id} className="p-4">
            <div className="flex items-center gap-4">
              <div className="flex flex-col gap-1">
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => moveUp(index)}
                  disabled={index === 0}
                  className="h-8 w-8 p-0"
                >
                  <Icon name="ChevronUp" size={16} />
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => moveDown(index)}
                  disabled={index === menuItems.length - 1}
                  className="h-8 w-8 p-0"
                >
                  <Icon name="ChevronDown" size={16} />
                </Button>
              </div>

              <div className="w-10 h-10 rounded-lg bg-primary/10 flex items-center justify-center">
                <Icon name={item.icon as any} size={20} className="text-primary" />
              </div>

              <div className="flex-1">
                <h3 className="font-semibold">{item.label}</h3>
                <p className="text-sm text-muted-foreground">{item.route}</p>
              </div>

              <div className="flex items-center gap-2">
                <span className="text-sm text-muted-foreground">
                  {item.isVisible ? 'Видимый' : 'Скрытый'}
                </span>
                <Switch
                  checked={item.isVisible}
                  onCheckedChange={() => toggleVisibility(item.id)}
                />
              </div>

              <div className="text-sm text-muted-foreground w-12 text-center">
                #{item.orderPosition}
              </div>
            </div>
          </Card>
        ))}
      </div>
    </div>
  );
};

export default MenuManagement;
