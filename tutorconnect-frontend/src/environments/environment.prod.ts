export const environment = {
  production: true,
  // Relative path, not a hardcoded host: once deployed, the Angular build is served
  // by the same App Service as the API (same origin), so this just resolves against
  // whatever domain the app is actually running on - Azure's default *.azurewebsites.net
  // URL today, a custom domain later, with no rebuild needed either way.
  apiUrl: '/api'
};
