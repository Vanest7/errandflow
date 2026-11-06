import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { AuthService as Auth0Service, User } from '@auth0/auth0-angular';
import {
  catchError,
  combineLatest,
  distinctUntilChanged,
  finalize,
  map,
  Observable,
  of,
  switchMap,
  tap,
} from 'rxjs';
import { AuthMeResponse, CurrentUserDto } from '../api/generated';
import { AuthApiService } from './data-access/auth-api.service';

@Injectable({ providedIn: 'root' })
export class AuthFacade {
  private readonly auth0 = inject(Auth0Service);
  private readonly authApi = inject(AuthApiService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly auth0User = toSignal(this.auth0.user$, {
    initialValue: null,
  });
  private readonly auth0Error = toSignal(this.auth0.error$, {
    initialValue: undefined,
  });
  private readonly currentUser = signal<CurrentUserDto | null>(null);
  private readonly apiIdentity = signal<AuthMeResponse | null>(null);
  private readonly profileError = signal<string | null>(null);
  private readonly profileSyncInProgress = signal(false);

  readonly user = this.auth0User;
  readonly registeredUser = this.currentUser.asReadonly();
  readonly identity = this.apiIdentity.asReadonly();
  readonly isLoading = toSignal(this.auth0.isLoading$, {
    initialValue: true,
  });
  readonly isAuthenticated = toSignal(this.auth0.isAuthenticated$, {
    initialValue: false,
  });
  readonly error = computed(() => this.auth0Error()?.message ?? this.profileError());
  readonly isSynchronizing = computed(() => this.isAuthenticated() && this.profileSyncInProgress());

  constructor() {
    this.synchronizeProfile().pipe(takeUntilDestroyed(this.destroyRef)).subscribe();
  }

  login(): void {
    this.auth0.loginWithRedirect().subscribe({
      error: (error: unknown) => {
        console.error('Auth0 login failed:', error);
      },
    });
  }

  logout(): void {
    this.auth0
      .logout({
        logoutParams: {
          returnTo: window.location.origin,
        },
      })
      .subscribe();
  }

  refreshIdentity(): void {
    if (!this.isAuthenticated()) {
      return;
    }

    this.loadIdentity().pipe(takeUntilDestroyed(this.destroyRef)).subscribe();
  }

  private synchronizeProfile(): Observable<AuthMeResponse | null> {
    return combineLatest([this.auth0.isAuthenticated$, this.auth0.user$]).pipe(
      map(([isAuthenticated, user]) => ({
        isAuthenticated,
        user: isAuthenticated ? user : null,
      })),
      distinctUntilChanged(
        (previous, current) =>
          previous.isAuthenticated === current.isAuthenticated &&
          previous.user?.sub === current.user?.sub,
      ),
      switchMap(({ isAuthenticated, user }) => {
        this.resetProfile();
        return isAuthenticated && user ? this.registerProfile(user) : of(null);
      }),
    );
  }

  private registerProfile(user: User): Observable<AuthMeResponse | null> {
    this.profileSyncInProgress.set(true);

    return this.authApi.registerCurrentUser(resolveDisplayName(user)).pipe(
      tap((profile) => this.currentUser.set(profile)),
      switchMap(() => this.loadIdentity()),
      catchError((error: unknown) => {
        this.profileError.set('Unable to synchronize your user profile.');
        console.error('Profile synchronization failed:', error);
        return of(null);
      }),
      finalize(() => this.profileSyncInProgress.set(false)),
    );
  }

  private loadIdentity(): Observable<AuthMeResponse | null> {
    this.profileSyncInProgress.set(true);

    return this.authApi.getIdentity().pipe(
      tap((identity) => {
        this.apiIdentity.set(identity);
        this.profileError.set(null);
      }),
      catchError((error: unknown) => {
        this.profileError.set('Unable to load your API identity.');
        console.error('API identity check failed:', error);
        return of(null);
      }),
      finalize(() => this.profileSyncInProgress.set(false)),
    );
  }

  private resetProfile(): void {
    this.currentUser.set(null);
    this.apiIdentity.set(null);
    this.profileError.set(null);
  }
}

function resolveDisplayName(user: User): string {
  return user.name ?? user.nickname ?? user.email ?? user.sub ?? 'ErrandFlow user';
}
