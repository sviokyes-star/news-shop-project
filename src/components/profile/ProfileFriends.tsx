import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import Icon from '@/components/ui/icon';
import PlayerLink from '@/components/ui/player-link';

interface FriendRequest {
  steamId: string;
  personaName: string;
  avatarUrl: string;
  createdAt: string;
}

interface Friend {
  steamId: string;
  personaName: string;
  avatarUrl: string;
  isOnline: boolean;
  lastOnline: string;
}

interface ProfileFriendsProps {
  friends: Friend[];
  friendRequests: FriendRequest[];
  onAccept: (steamId: string) => void;
  onDecline: (steamId: string) => void;
}

export default function ProfileFriends({ friends, friendRequests, onAccept, onDecline }: ProfileFriendsProps) {
  return (
    <>
      {friendRequests.length > 0 && (
        <Card className="p-6 border border-primary/30 bg-primary/5">
          <div className="flex items-center gap-2 mb-4">
            <Icon name="UserPlus" size={20} className="text-primary" />
            <h2 className="text-lg font-bold">Заявки в друзья</h2>
            <span className="ml-auto px-2 py-0.5 bg-primary text-primary-foreground text-xs font-bold rounded-full">
              {friendRequests.length}
            </span>
          </div>
          <div className="space-y-3">
            {friendRequests.map(req => (
              <div key={req.steamId} className="flex items-center gap-3">
                <PlayerLink steamId={req.steamId} name={req.personaName} avatarOnly avatarUrl={req.avatarUrl} avatarSize={10} />
                <PlayerLink steamId={req.steamId} name={req.personaName} className="flex-1" />
                <div className="flex gap-2">
                  <Button size="sm" onClick={() => onAccept(req.steamId)} className="gap-1">
                    <Icon name="Check" size={14} />
                    Принять
                  </Button>
                  <Button size="sm" variant="outline" onClick={() => onDecline(req.steamId)} className="gap-1">
                    <Icon name="X" size={14} />
                  </Button>
                </div>
              </div>
            ))}
          </div>
        </Card>
      )}

      <div className="space-y-6">
        <div>
          <h2 className="text-3xl font-bold mb-2">Друзья</h2>
          <p className="text-muted-foreground">Ваши друзья на сайте</p>
        </div>

        {friends.length > 0 ? (
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-4">
            {friends.map(f => (
              <div key={f.steamId} className="relative">
                <PlayerLink
                  steamId={f.steamId}
                  name={f.personaName}
                  showAvatar
                  avatarUrl={f.avatarUrl}
                  avatarSize={12}
                  isOnline={f.isOnline}
                  className="flex-col items-center gap-2 p-4 w-full rounded-xl bg-card border border-border hover:border-primary/50 transition-all text-center"
                />
              </div>
            ))}
          </div>
        ) : (
          <Card className="p-12 text-center border border-dashed border-border bg-card/30">
            <Icon name="Users" size={48} className="text-muted-foreground mx-auto mb-4" />
            <p className="text-xl text-muted-foreground mb-2">У вас пока нет друзей</p>
            <p className="text-muted-foreground">Найдите игроков и отправьте им заявку в друзья</p>
          </Card>
        )}
      </div>
    </>
  );
}
