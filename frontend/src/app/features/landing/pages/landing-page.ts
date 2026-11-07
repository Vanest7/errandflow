import { Component, inject } from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';
import { AuthFacade } from '../../../core/auth/auth.facade';

@Component({
  selector: 'app-landing-page',
  imports: [TranslocoDirective],
  templateUrl: './landing-page.html',
})
export class LandingPage {
  protected readonly auth = inject(AuthFacade);
}
