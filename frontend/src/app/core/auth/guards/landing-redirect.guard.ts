import { inject } from '@angular/core';
import { AuthService } from '@auth0/auth0-angular';
import { CanActivateFn, Router } from '@angular/router';
import { combineLatest, filter, map, take } from 'rxjs';

export const landingRedirectGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return combineLatest([auth.isLoading$, auth.isAuthenticated$]).pipe(
    filter(([isLoading]) => !isLoading),
    take(1),
    map(([, isAuthenticated]) =>
      isAuthenticated ? router.createUrlTree(['/plans']) : true,
    ),
  );
};
