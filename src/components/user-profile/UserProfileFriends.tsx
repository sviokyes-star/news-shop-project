import PlayerLink from '@/components/ui/player-link';
import { Card } from '@/components/ui/card';
import Icon from '@/components/ui/icon';

interface Friend {
  steamId: string;
  personaName: string;
  avatarUrl: string;
  isOnline: boolean;
  lastOnline: string;
}

interface UserProfileFriendsProps {
  friends: Friend[];
}

export default function UserProfileFriends({ friends }: UserProfileFriendsProps) {
  return (
    <div className="space-y-4">
      <h2 className="text-2xl font-bold">Друзья <span className="text-muted-foreground text-lg font-normal">({friends.length})</span></h2>
      {friends.length > 0 ? (
        <div className="flex flex-wrap gap-3">
          {friends.map(f => (
            <div key={f.steamId} className="relative">
              <PlayerLink
                steamId={f.steamId}
                name={f.personaName}
                showAvatar
                avatarUrl={f.avatarUrl}
                avatarSize={10}
                isOnline={f.isOnline}
                className="flex-col items-center gap-1 p-3 rounded-xl bg-card border border-border hover:border-primary/50 transition-all"
              />
            </div>
          ))}
        </div>
      ) : (
        <Card className="p-8 text-center border border-dashed border-border bg-card/30">
          <Icon name="Users" size={36} className="text-muted-foreground mx-auto mb-2" />
          <p className="text-muted-foreground">У пользователя пока нет друзей</p>
        </Card>
      )}
    </div>
  );
}