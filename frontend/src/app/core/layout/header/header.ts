import { Component, inject } from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';
import { AuthFacade } from '../../auth/auth.facade';

@Component({
  selector: 'app-header',
  imports: [TranslocoDirective],
  templateUrl: 'header.html',
})
export class Header {
  protected readonly auth = inject(AuthFacade);
}
