export const getTimeUntilStart = (dateString: string) => {
  const start = new Date(dateString).getTime();
  const now = Date.now();
  const diff = start - now;

  if (diff <= 0) return null;

  const days = Math.floor(diff / (1000 * 60 * 60 * 24));
  const hours = Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
  const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
  const seconds = Math.floor((diff % (1000 * 60)) / 1000);

  return { days, hours, minutes, seconds };
};

export const getConfirmationTimeLeft = (dateString: string) => {
  const start = new Date(dateString).getTime();
  const now = Date.now();
  const oneHourBefore = start - (60 * 60 * 1000);
  const diff = start - now;

  if (diff <= 0 || now < oneHourBefore) return null;

  const minutes = Math.floor(diff / (1000 * 60));
  const seconds = Math.floor((diff % (1000 * 60)) / 1000);

  return { minutes, seconds };
};

export const isConfirmationActive = (dateString: string) => {
  const start = new Date(dateString).getTime();
  const now = Date.now();
  const oneHourBefore = start - (60 * 60 * 1000);
  
  return now >= oneHourBefore && now < start;
};

export const isRegistrationClosed = (dateString: string) => {
  const start = new Date(dateString).getTime();
  const now = Date.now();
  
  return now >= start;
};

export const getTimeUntilConfirmation = (dateString: string) => {
  const start = new Date(dateString).getTime();
  const now = Date.now();
  const oneHourBefore = start - (60 * 60 * 1000);
  const diff = oneHourBefore - now;

  if (diff <= 0 || now >= start) return null;

  const days = Math.floor(diff / (1000 * 60 * 60 * 24));
  const hours = Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
  const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
  const seconds = Math.floor((diff % (1000 * 60)) / 1000);

  return { days, hours, minutes, seconds };
};