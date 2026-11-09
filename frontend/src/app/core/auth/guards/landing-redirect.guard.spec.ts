import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, provideRouter, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { AuthService } from '@auth0/auth0-angular';
import { firstValueFrom, Observable, of } from 'rxjs';
import { landingRedirectGuard } from './landing-redirect.guard';

describe('landingRedirectGuard', () => {
  async function evaluate(authenticated: boolean): Promise<boolean | UrlTree> {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: {
            isLoading$: of(false),
            isAuthenticated$: of(authenticated),
          },
        },
      ],
    });

    const result = TestBed.runInInjectionContext(() =>
      landingRedirectGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
    );

    return firstValueFrom(result as Observable<boolean | UrlTree>);
  }

  afterEach(() => TestBed.resetTestingModule());

  it('allows signed-out visitors to open the landing page', async () => {
    expect(await evaluate(false)).toBe(true);
  });

  it('redirects signed-in visitors to their plans', async () => {
    const redirect = await evaluate(true);
    expect(redirect instanceof UrlTree).toBe(true);
    const router = TestBed.inject(Router);
    expect(router.serializeUrl(redirect as UrlTree)).toBe('/plans');
  });
});
