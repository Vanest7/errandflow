import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Translation, TranslocoLoader, provideTransloco } from '@jsverse/transloco';
import { App } from './app';
import { AuthFacade } from './core/auth/auth.facade';
import { ThemeService } from './core/theme/theme.service';

class TestTranslocoLoader implements TranslocoLoader {
  getTranslation() {
    return Promise.resolve<Translation>({ app: { welcome: 'Bienvenido' } });
  }
}

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
          loader: TestTranslocoLoader,
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
            retryProfileSynchronization: () => undefined,
          },
        },
        {
          provide: ThemeService,
          useValue: {
            activeTheme: signal('light'),
            isDark: signal(false),
            setTheme: () => undefined,
            toggleTheme: () => undefined,
            useSystemTheme: () => undefined,
          },
        },
      ],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render the router outlet while signed out', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('router-outlet')).not.toBeNull();
    expect(root.textContent).not.toContain('API Identity');
  });
});
