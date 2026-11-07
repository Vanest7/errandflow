import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';

export type Theme = 'light' | 'dark';
export type ThemePreference = Theme | 'system';

@Injectable({
  providedIn: 'root',
})
export class ThemeService {
  private readonly destroyRef = inject(DestroyRef);
  private readonly storageKey = 'errandflow.theme';
  private readonly mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');

  private readonly preference = signal<ThemePreference>(this.readPreference());
  private readonly systemDark = signal(this.mediaQuery.matches);

  readonly activeTheme = computed<Theme>(() => {
    const preference = this.preference();

    if (preference === 'system') {
      return this.systemDark() ? 'dark' : 'light';
    }

    return preference;
  });

  readonly isDark = computed(() => this.activeTheme() === 'dark');

  constructor() {
    this.applyTheme();

    const handleSystemChange = (event: MediaQueryListEvent): void => {
      this.systemDark.set(event.matches);
      this.applyTheme();
    };

    this.mediaQuery.addEventListener('change', handleSystemChange);

    this.destroyRef.onDestroy(() => {
      this.mediaQuery.removeEventListener('change', handleSystemChange);
    });
  }

  setTheme(theme: Theme): void {
    this.preference.set(theme);
    localStorage.setItem(this.storageKey, theme);
    this.applyTheme();
  }

  toggleTheme(): void {
    this.setTheme(this.isDark() ? 'light' : 'dark');
  }

  useSystemTheme(): void {
    this.preference.set('system');
    localStorage.removeItem(this.storageKey);
    this.applyTheme();
  }

  private readPreference(): ThemePreference {
    const stored = localStorage.getItem(this.storageKey);

    return stored === 'light' || stored === 'dark' ? stored : 'system';
  }

  private applyTheme(): void {
    document.documentElement.dataset['theme'] = this.activeTheme();
  }
}
