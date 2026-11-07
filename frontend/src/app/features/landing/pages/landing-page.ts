import { Component, inject } from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';
import { AuthFacade } from '../../../core/auth/auth.facade';
import {ThemeService} from '../../../core/theme/theme.service';

@Component({
  selector: 'app-landing-page',
  imports: [TranslocoDirective],
  templateUrl: './landing-page.html',
})
export class LandingPage {
  protected readonly auth = inject(AuthFacade);
  protected readonly theme = inject(ThemeService);
}
