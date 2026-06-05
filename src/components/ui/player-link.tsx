import { useNavigate } from 'react-router-dom';

interface PlayerLinkProps {
  steamId: string;
  name: string;
  avatarUrl?: string | null;
  className?: string;
  showAvatar?: boolean;
  avatarOnly?: boolean;
  avatarSize?: number;
  isOnline?: boolean;
}

const PlayerLink = ({ steamId, name, avatarUrl, className = '', showAvatar = false, avatarOnly = false, avatarSize = 8, isOnline }: PlayerLinkProps) => {
  const navigate = useNavigate();

  const handleClick = (e: React.MouseEvent) => {
    e.stopPropagation();
    navigate(`/profile/${steamId}`);
  };

  const showAvatarImg = (showAvatar || avatarOnly) && avatarUrl;

  return (
    <button
      onClick={handleClick}
      className={`inline-flex items-center gap-2 hover:text-primary transition-colors cursor-pointer ${className}`}
    >
      {showAvatarImg && (
        <span className="relative inline-flex flex-shrink-0">
          <img
            src={avatarUrl!}
            alt={name}
            className={`w-${avatarSize} h-${avatarSize} rounded-full`}
          />
          {isOnline !== undefined && (
            <span className={`absolute -bottom-0.5 -right-0.5 w-2.5 h-2.5 rounded-full border-2 border-background ${isOnline ? 'bg-green-500' : 'bg-muted-foreground'}`} />
          )}
        </span>
      )}
      {!avatarOnly && (
        <span className="font-semibold hover:underline">{name}</span>
      )}
    </button>
  );
};

export default PlayerLink;