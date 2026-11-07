import { Component, inject } from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';
import { AuthFacade } from '../../auth/auth.facade';
import { LanguageService } from '../../../i18n/language.service';
import { ThemeToggle } from '../../../shared/ui/theme-toggle/theme-toggle';

@Component({
  selector: 'app-header',
  imports: [TranslocoDirective, ThemeToggle],
  templateUrl: 'header.html',
})
export class Header {
  protected readonly auth = inject(AuthFacade);
  protected readonly language = inject(LanguageService);
}
