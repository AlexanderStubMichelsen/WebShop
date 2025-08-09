export const getApiUrl = () => {
  const isProduction = typeof window !== 'undefined' && window.location.hostname === 'shop.devdisplay.online';
  return isProduction 
    ? 'https://webshop-api.devdisplay.online' 
    : 'http://localhost:5195';
};