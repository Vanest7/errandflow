import { TestBed } from '@angular/core/testing';
import { AuthService as Auth0Service, User } from '@auth0/auth0-angular';
import { BehaviorSubject, of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthMeResponse, CurrentUserDto } from '../api/generated';
import { AuthFacade } from './auth.facade';
import { AuthApiService } from './data-access/auth-api.service';

describe('AuthFacade', () => {
  const user: User = {
    sub: 'auth0|123',
    name: 'Ada Lovelace',
  };
  const registeredUser: CurrentUserDto = {
    id: '0f3b7a36-1b7f-4d8e-9e6a-3c5f7c9d2a10',
    displayName: 'Ada Lovelace',
    preferredLanguage: 'en',
  };
  const identity: AuthMeResponse = {
    externalUserId: 'auth0|123',
    localUserId: registeredUser.id,
    isRegistered: true,
  };

  let isAuthenticated$: BehaviorSubject<boolean>;
  let user$: BehaviorSubject<User | null | undefined>;
  let authApi: {
    registerCurrentUser: ReturnType<typeof vi.fn>;
    getIdentity: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    isAuthenticated$ = new BehaviorSubject(false);
    user$ = new BehaviorSubject<User | null | undefined>(null);
    authApi = {
      registerCurrentUser: vi.fn(() => of(registeredUser)),
      getIdentity: vi.fn(() => of(identity)),
    };

    TestBed.configureTestingModule({
      providers: [
        {
          provide: Auth0Service,
          useValue: {
            isAuthenticated$,
            user$,
            isLoading$: new BehaviorSubject(false),
            error$: new Subject<Error>(),
            loginWithRedirect: () => of(undefined),
            logout: () => of(undefined),
          },
        },
        {
          provide: AuthApiService,
          useValue: authApi,
        },
      ],
    });
  });

  it('synchronizes the local profile after authentication', () => {
    const facade = TestBed.inject(AuthFacade);

    isAuthenticated$.next(true);
    user$.next(user);

    expect(authApi.registerCurrentUser).toHaveBeenCalledWith('Ada Lovelace');
    expect(authApi.getIdentity).toHaveBeenCalled();
    expect(facade.registeredUser()).toEqual(registeredUser);
    expect(facade.identity()).toEqual(identity);
    expect(facade.isSynchronizing()).toBe(false);
  });

  it('clears the local profile after logout', () => {
    const facade = TestBed.inject(AuthFacade);

    isAuthenticated$.next(true);
    user$.next(user);
    isAuthenticated$.next(false);
    user$.next(null);

    expect(facade.registeredUser()).toBeNull();
    expect(facade.identity()).toBeNull();
  });
});
