import { Injectable, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslocoService } from '@jsverse/transloco';

export type SupportedLanguage = 'es' | 'en';

@Injectable({
  providedIn: 'root',
})
export class LanguageService {
  private readonly transloco = inject(TranslocoService);
  private readonly storageKey = 'errandflow.language';

  readonly activeLanguage = toSignal(this.transloco.langChanges$, {
    initialValue: this.transloco.getActiveLang(),
  });

  constructor() {
    const storedLanguage = localStorage.getItem(this.storageKey);

    const language = storedLanguage === 'es' || storedLanguage === 'en' ? storedLanguage : 'es';

    this.transloco.setActiveLang(language);
    document.documentElement.lang = language;
  }

  setLanguage(language: string): void {
    if (language !== 'es' && language !== 'en') {
      return;
    }

    if (language === this.activeLanguage()) {
      return;
    }

    this.transloco.setActiveLang(language);

    localStorage.setItem(this.storageKey, language);

    document.documentElement.lang = language;
  }
}
