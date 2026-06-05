import PlayerLink from '@/components/ui/player-link';

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
  if (friends.length === 0) return null;

  return (
    <div className="space-y-4">
      <h2 className="text-2xl font-bold">Друзья <span className="text-muted-foreground text-lg font-normal">({friends.length})</span></h2>
      <div className="flex flex-wrap gap-3">
        {friends.map(f => (
          <div key={f.steamId} className="relative">
            <PlayerLink
              steamId={f.steamId}
              name={f.personaName}
              showAvatar
              avatarUrl={f.avatarUrl}
              avatarSize={10}
              className="flex-col items-center gap-1 p-3 rounded-xl bg-card border border-border hover:border-primary/50 transition-all"
            />
            <div className={`absolute top-2 right-2 w-3 h-3 rounded-full border-2 border-background ${f.isOnline ? 'bg-green-500' : 'bg-muted-foreground'}`} />
          </div>
        ))}
      </div>
    </div>
  );
}
