import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTransloco } from '@jsverse/transloco';
import { App } from './app';
import { AuthFacade } from './core/auth/auth.facade';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter([]),
        provideTransloco({
          config: {
            availableLangs: ['es'],
            defaultLang: 'es',
            fallbackLang: 'es',
          },
          loader: {
            getTranslation: () => Promise.resolve({ app: { welcome: 'Bienvenido' } }),
          },
        }),
        {
          provide: AuthFacade,
          useValue: {
            user: signal(null),
            registeredUser: signal(null),
            identity: signal(null),
            isLoading: signal(false),
            isAuthenticated: signal(false),
            isSynchronizing: signal(false),
            error: signal<string | null>(null),
            login: () => undefined,
            logout: () => undefined,
            refreshIdentity: () => undefined,
          },
        },
      ],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render the application title', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('ErrandFlow');
  });
});
