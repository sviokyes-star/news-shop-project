import { Card } from '@/components/ui/card';
import Icon from '@/components/ui/icon';

export default function AdminLoadingState() {
  return (
    <div className="min-h-screen bg-gradient-to-br from-background via-background to-primary/5 flex items-center justify-center">
      <Card className="p-5 sm:p-8 bg-card/80 backdrop-blur border-primary/20">
        <div className="flex items-center gap-3">
          <Icon name="Loader2" size={24} className="animate-spin" />
          <span className="text-lg">Проверка доступа...</span>
        </div>
      </Card>
    </div>
  );
}
