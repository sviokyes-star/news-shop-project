import { Card } from '@/components/ui/card';
import Icon from '@/components/ui/icon';
import { getTimeUntilStart } from './utils';

interface CountdownTimerProps {
  startDate: string;
}

const CountdownTimer = ({ startDate }: CountdownTimerProps) => {
  const timeLeft = getTimeUntilStart(startDate);

  if (!timeLeft) return null;

  return (
    <Card className="p-4 sm:p-6 bg-gradient-to-r from-primary/5 to-primary/10 border-primary/20">
      <div className="space-y-4">
        <div className="flex items-center gap-2">
          <Icon name="Clock" size={24} className="text-primary" />
          <h2 className="text-xl sm:text-2xl font-bold">До начала турнира</h2>
        </div>
        <div className="flex gap-2 sm:gap-4 justify-around">
          {timeLeft.days > 0 && (
            <div className="text-center">
              <div className="text-3xl sm:text-5xl font-bold text-primary">{timeLeft.days}</div>
              <div className="text-xs sm:text-sm text-muted-foreground mt-1">дней</div>
            </div>
          )}
          <div className="text-center">
            <div className="text-3xl sm:text-5xl font-bold text-primary">{String(timeLeft.hours).padStart(2, '0')}</div>
            <div className="text-xs sm:text-sm text-muted-foreground mt-1">часов</div>
          </div>
          <div className="text-center">
            <div className="text-3xl sm:text-5xl font-bold text-primary">{String(timeLeft.minutes).padStart(2, '0')}</div>
            <div className="text-xs sm:text-sm text-muted-foreground mt-1">минут</div>
          </div>
          <div className="text-center">
            <div className="text-3xl sm:text-5xl font-bold text-primary">{String(timeLeft.seconds).padStart(2, '0')}</div>
            <div className="text-xs sm:text-sm text-muted-foreground mt-1">секунд</div>
          </div>
        </div>
      </div>
    </Card>
  );
};

export default CountdownTimer;
