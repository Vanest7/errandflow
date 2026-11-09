import { authGuardFn } from '@auth0/auth0-angular';
import { Routes } from '@angular/router';
import { landingRedirectGuard } from './core/auth/guards/landing-redirect.guard';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    canActivate: [landingRedirectGuard],
    loadComponent: () =>
      import('./features/landing/pages/landing-page').then((module) => module.LandingPage),
  },
  {
    path: 'plans',
    pathMatch: 'full',
    canActivate: [authGuardFn],
    loadComponent: () =>
      import('./features/plans/pages/plans-home/plans-home').then((module) => module.PlansHome),
  },
  {
    path: 'plans/:planId',
    canActivate: [authGuardFn],
    loadComponent: () =>
      import('./features/plans/pages/plan-editor/plan-editor').then((module) => module.PlanEditor),
  },
  { path: '**', redirectTo: '' },
];
