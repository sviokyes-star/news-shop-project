import { useState, useEffect } from 'react';
import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import Icon from '@/components/ui/icon';
import func2url from '../../backend/func2url.json';

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

const PartnersTab = () => {
  const cachedPartners = localStorage.getItem('partners');
  const [partners, setPartners] = useState<Partner[]>(
    cachedPartners ? JSON.parse(cachedPartners) : []
  );
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    loadPartners();
  }, []);

  const loadPartners = async () => {
    try {
      const response = await fetch(func2url.partners);
      const data = await response.json();
      setPartners(data.partners || []);
      localStorage.setItem('partners', JSON.stringify(data.partners || []));
    } catch (error) {
      console.error('Failed to load partners:', error);
    }
  };

  const categories = Array.from(new Set(partners.map(p => p.category)));

  return (
      <div className="space-y-10">
        <div className="space-y-3">
          <div className="inline-block px-4 py-1.5 bg-primary/10 border border-primary/20 rounded-full mb-2">
            <span className="text-sm font-medium text-primary">Партнёрство</span>
          </div>
          <p className="text-muted-foreground text-xl">
            Компании и проекты, с которыми мы работаем
          </p>
        </div>


        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {partners.map((partner) => (
                  <Card
                    key={partner.id}
                    className="p-6 bg-card/80 backdrop-blur border-primary/20 hover:border-primary/40 transition-all duration-300 hover:shadow-lg hover:shadow-primary/10"
                  >
                    <div className="flex flex-col items-center text-center space-y-4">
                      <div className="w-20 h-20 rounded-2xl bg-primary/10 flex items-center justify-center text-5xl overflow-hidden">
                        {partner.logo && partner.logo.startsWith('http') ? (
                          <img src={partner.logo} alt={partner.name} className="w-full h-full object-cover" />
                        ) : (
                          partner.logo || '🤝'
                        )}
                      </div>
                      
                      <div className="space-y-2">
                        <h4 className="text-xl font-bold">{partner.name}</h4>
                        <p className="text-sm text-muted-foreground">
                          {partner.description}
                        </p>
                      </div>

                      <Button
                        variant="outline"
                        className="w-full gap-2"
                        onClick={() => window.open(partner.website, '_blank')}
                      >
                        <Icon name="ExternalLink" size={16} />
                        Перейти на сайт
                      </Button>
                    </div>
                  </Card>
          ))}
        </div>

        <Card className="p-6 bg-gradient-to-r from-primary/10 to-primary/5 border-primary/20">
          <div className="flex items-center justify-between gap-6">
            <div className="flex items-center gap-4">
              <div className="w-12 h-12 rounded-full bg-primary/20 flex items-center justify-center flex-shrink-0">
                <Icon name="Handshake" size={24} className="text-primary" />
              </div>
              <div>
                <h3 className="text-xl font-bold">Хотите стать партнёром?</h3>
                <p className="text-sm text-muted-foreground">
                  Свяжитесь с нами, чтобы обсудить возможности партнёрства
                </p>
              </div>
            </div>
            <Button className="gap-2 flex-shrink-0">
              <Icon name="Mail" size={16} />
              Связаться
            </Button>
          </div>
        </Card>
      </div>
  );
};

export default PartnersTab;