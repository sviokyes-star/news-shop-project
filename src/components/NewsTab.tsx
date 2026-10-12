import { useNavigate } from 'react-router-dom';
import { Card } from '@/components/ui/card';
import Icon from '@/components/ui/icon';

interface NewsItem {
  id: number;
  title: string;
  description: string;
  date: string;
}

interface NewsTabProps {
  newsItems: NewsItem[];
}

const NewsTab = ({ newsItems }: NewsTabProps) => {
  const navigate = useNavigate();

  return (
    <div className="space-y-6 sm:space-y-10">
      <div className="space-y-3">
        <div className="inline-block px-4 py-1.5 bg-primary/10 border border-primary/20 rounded-full mb-2">
          <span className="text-sm font-medium text-primary">Последние обновления</span>
        </div>
        <p className="text-muted-foreground text-xl">События и обновления проекта</p>
      </div>

      
      <div className="grid gap-6">
        {newsItems.map((item, index) => (
          <Card 
            key={item.id} 
            className="group p-5 sm:p-8 border border-border hover:border-primary/50 transition-all duration-300 cursor-pointer hover:shadow-2xl hover:shadow-primary/10 hover:-translate-y-1 bg-card/50 backdrop-blur"
            style={{ animationDelay: `${index * 100}ms` }}
            onClick={() => navigate(`/news/${item.id}`)}
          >
            <div className="space-y-4">
              <div className="flex items-start justify-between gap-4">
                <div className="space-y-2 flex-1">
                  <h3 className="text-2xl font-bold tracking-tight group-hover:text-primary transition-colors">
                    {item.title}
                  </h3>
                  <p className="text-muted-foreground text-lg leading-relaxed">{item.description}</p>
                </div>
                <div className="w-12 h-12 rounded-full bg-secondary flex items-center justify-center flex-shrink-0 group-hover:bg-primary group-hover:text-primary-foreground transition-all duration-300">
                  <Icon name="ArrowRight" size={24} />
                </div>
              </div>
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <Icon name="Calendar" size={16} />
                <span>{item.date}</span>
              </div>
            </div>
          </Card>
        ))}
      </div>
    </div>
  );
};

export default NewsTab;