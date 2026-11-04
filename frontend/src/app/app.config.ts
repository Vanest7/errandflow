import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { provideAuth0 } from '@auth0/auth0-angular';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { authHttpInterceptorFn } from '@auth0/auth0-angular';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(), provideRouter(routes),
    provideHttpClient(withInterceptors([authHttpInterceptorFn])),
    provideAuth0({
  domain: 'dev-ufeqgt5qbnghre8j.us.auth0.com',
  clientId: 'SY0tJAoNGOqn8TcGmQ57Ggkg66aZtPqP',
  authorizationParams: {
    redirect_uri: window.location.origin,
    audience: 'https://api.errandflow.local',
  },
  httpInterceptor: {
  allowedList: ['https://localhost:7094/api/*'],
},
}),
  ],
};
