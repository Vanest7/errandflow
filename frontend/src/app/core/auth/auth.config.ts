import { AuthConfig } from '@auth0/auth0-angular';
import { API_ROOT_URL } from '../config/api.config';

export const auth0Config: AuthConfig = {
  domain: 'dev-ufeqgt5qbnghre8j.us.auth0.com',
  clientId: 'SY0tJAoNGOqn8TcGmQ57Ggkg66aZtPqP',
  authorizationParams: {
    redirect_uri: window.location.origin,
    audience: 'https://api.errandflow.local',
  },
  httpInterceptor: {
    allowedList: [`${API_ROOT_URL}/api/*`],
  },
};
