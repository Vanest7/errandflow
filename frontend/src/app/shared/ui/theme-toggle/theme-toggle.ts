import { Component, inject } from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';
import { ThemeService } from '../../../core/theme/theme.service';

@Component({
  selector: 'app-theme-toggle',
  imports: [TranslocoDirective],
  templateUrl: './theme-toggle.html',
  styleUrl: './theme-toggle.scss',
})
export class ThemeToggle {
  protected readonly theme = inject(ThemeService);

  private pointerStartX: number | null = null;
  private suppressClick = false;

  protected onPointerDown(event: PointerEvent): void {
    if (event.button !== 0) {
      return;
    }

    this.pointerStartX = event.clientX;
    this.suppressClick = false;
  }

  protected onPointerUp(event: PointerEvent): void {
    if (this.pointerStartX === null) {
      return;
    }

    const distance = event.clientX - this.pointerStartX;
    this.pointerStartX = null;

    if (Math.abs(distance) < 20) {
      return;
    }

    this.suppressClick = true;
    this.theme.setTheme(distance > 0 ? 'dark' : 'light');
  }

  protected onPointerCancel(): void {
    this.pointerStartX = null;
    this.suppressClick = false;
  }

  protected onClick(): void {
    if (this.suppressClick) {
      this.suppressClick = false;
      return;
    }

    this.theme.toggleTheme();
  }

  protected onArrowLeft(event: Event): void {
    event.preventDefault();
    this.theme.setTheme('light');
  }

  protected onArrowRight(event: Event): void {
    event.preventDefault();
    this.theme.setTheme('dark');
  }
}
