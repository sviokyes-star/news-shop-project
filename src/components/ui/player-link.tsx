import { useNavigate } from 'react-router-dom';

interface PlayerLinkProps {
  steamId: string;
  name: string;
  avatarUrl?: string | null;
  className?: string;
  showAvatar?: boolean;
  avatarOnly?: boolean;
  avatarSize?: number;
}

const PlayerLink = ({ steamId, name, avatarUrl, className = '', showAvatar = false, avatarOnly = false, avatarSize = 8 }: PlayerLinkProps) => {
  const navigate = useNavigate();

  const handleClick = (e: React.MouseEvent) => {
    e.stopPropagation();
    navigate(`/profile/${steamId}`);
  };

  return (
    <button
      onClick={handleClick}
      className={`inline-flex items-center gap-2 hover:text-primary transition-colors cursor-pointer ${className}`}
    >
      {(showAvatar || avatarOnly) && avatarUrl && (
        <img
          src={avatarUrl}
          alt={name}
          className={`w-${avatarSize} h-${avatarSize} rounded-full flex-shrink-0`}
        />
      )}
      {!avatarOnly && <span className="font-semibold hover:underline">{name}</span>}
    </button>
  );
};

export default PlayerLink;