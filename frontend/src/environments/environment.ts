export const environment = {
  production: true,
  apiBaseUrl: '/api',
  entraId: {
    clientId: 'REPLACE_WITH_ENTRA_APP_CLIENT_ID',
    tenantId: 'REPLACE_WITH_ENTRA_TENANT_ID',
    apiScope: 'api://REPLACE_WITH_API_APP_ID/access_as_user',
    redirectUri: window.location.origin,
  },
};
