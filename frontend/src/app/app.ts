import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthFacade } from './core/auth/auth.facade';
import { TranslocoDirective } from '@jsverse/transloco';
import {Header} from "./core/layout/header/header";
import {LandingPage} from "./features/landing/pages/landing-page";

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, TranslocoDirective, Header, LandingPage],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  protected readonly title = signal('errandflow');
  protected readonly auth = inject(AuthFacade);
}
