import cloudUrls from '../../backend/func2url.json';

const SELF_HOSTED_BASE = (import.meta.env.VITE_API_BASE ?? '').replace(/\/$/, '');

type FunctionName =
  | keyof typeof cloudUrls
  | 'payment'
  | 'battlenet-auth'
  | 'upload-partner-logo';

const buildUrls = (): Record<string, string> => {
  if (SELF_HOSTED_BASE) {
    return new Proxy(
      {},
      {
        get: (_target, name: string) => `${SELF_HOSTED_BASE}/${name}`,
      },
    ) as Record<string, string>;
  }
  return cloudUrls as Record<string, string>;
};

export const apiUrl = buildUrls();

export const getApiUrl = (name: FunctionName): string => apiUrl[name as string];

export default apiUrl;
