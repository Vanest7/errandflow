import { AsyncPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthService } from '@auth0/auth0-angular';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { combineLatest, filter, switchMap, take } from 'rxjs';
import { AuthMeResponse } from './core/auth/models/auth-me-response.model';
import { CurrentUser } from './core/auth/models/current-user.model';

@Component({
  selector: 'app-root',
  imports: [AsyncPipe],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})

export class App {

 
private readonly http = inject(HttpClient);

protected readonly apiUser = signal<AuthMeResponse | null>(null);
protected readonly apiError = signal<string | null>(null);
protected checkApiIdentity(): void {
  this.http
    .get<AuthMeResponse>('https://localhost:7094/api/auth/me')
    .subscribe({
      next: (response) => {
        this.apiUser.set(response);
      },
      error: (error: HttpErrorResponse) => {
        console.error('API authentication check failed:', error);
      },
    });
}

  protected readonly title = signal('errandflow');
  protected readonly auth = inject(AuthService);

 constructor() {
  combineLatest([
    this.auth.isAuthenticated$,
    this.auth.user$,
  ])
    .pipe(
      filter(([isAuthenticated, user]) => isAuthenticated && !!user),
      take(1),
      switchMap(([, user]) =>
        this.http.post<CurrentUser>(
          'https://localhost:7094/api/auth/me',
          {
            displayName: user?.name ?? 'ErrandFlow user',
          },
        ),
      ),
      switchMap(() =>
        this.http.get<AuthMeResponse>(
          'https://localhost:7094/api/auth/me',
        ),
      ),
    )
    .subscribe({
      next: (response) => {
        this.apiUser.set(response);
        this.apiError.set(null);
      },
      error: (error: unknown) => {
        this.apiError.set('Unable to synchronize your user profile.');
        console.error('Profile synchronization failed:', error);
      },
    });
}

  protected login(): void {
    this.auth.loginWithRedirect().subscribe({
      error: (error: unknown) => {
        console.error('Auth0 login failed:', error);
      },
    });
  }

  protected logout(): void {
    this.auth.logout({
      logoutParams: {
        returnTo: window.location.origin,
      },
    }).subscribe();
  }
}