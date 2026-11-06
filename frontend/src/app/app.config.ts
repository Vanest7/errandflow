import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { provideAuth0 } from '@auth0/auth0-angular';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { authHttpInterceptorFn } from '@auth0/auth0-angular';
import { provideApiConfiguration } from './core/api/generated';
import { auth0Config } from './core/auth/auth.config';
import { API_ROOT_URL } from './core/config/api.config';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authHttpInterceptorFn])),
    provideApiConfiguration(API_ROOT_URL),
    provideAuth0(auth0Config),
  ],
};
