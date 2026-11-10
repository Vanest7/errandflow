#!/usr/bin/env python3
"""Apply the mobile plan-editor UX changes without replacing existing files."""

from __future__ import annotations

import argparse
import json
import re
from pathlib import Path


class PatchError(RuntimeError):
    pass


def replace_exact(value: str, old: str, new: str, label: str) -> str:
    count = value.count(old)
    if count != 1:
        raise PatchError(f"{label}: expected 1 occurrence, found {count}")
    return value.replace(old, new, 1)


def replace_button_class(value: str, click_handler: str, transform, label: str) -> str:
    """Update only the button with the requested click handler."""
    pattern = re.compile(
        r'<button\b(?:(?!</?button\b).)*?\(click\)="' + re.escape(click_handler) + r'"(?:(?!</?button\b).)*?>',
        re.DOTALL,
    )
    matches = list(pattern.finditer(value))
    if len(matches) != 1:
        raise PatchError(f"{label}: expected one button, found {len(matches)}")
    match = matches[0]
    button = match.group(0)
    class_match = re.search(r'class="([^"]+)"', button)
    if class_match is None:
        raise PatchError(f"{label}: button has no class attribute")
    current_class = class_match.group(1)
    changed_class = transform(current_class)
    if changed_class == current_class:
        raise PatchError(f"{label}: class was not changed")
    new_button = (
        button[:class_match.start(1)] + changed_class + button[class_match.end(1):]
    )
    return value[:match.start()] + new_button + value[match.end():]


MOBILE_NAV = '''    <!-- Mobile panel navigation -->
    <nav
      #mobileNavigation
      class="sticky top-20 z-20 mb-4 rounded-xl border border-[var(--ef-border)] bg-[var(--ef-surface)] p-1 shadow-sm lg:hidden"
      [attr.aria-label]="t('plans.mobile.navigation')"
    >
      <div class="grid grid-cols-2 gap-1">
        <button
          type="button"
          aria-controls="mobile-errands-panel"
          [attr.aria-pressed]="mobilePanel() === 'errands'"
          (click)="showMobilePanel('errands')"
          class="flex min-h-12 min-w-0 items-center justify-center gap-2 rounded-lg px-2 py-2 text-sm font-semibold transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--ef-primary)] aria-pressed:bg-[var(--ef-primary)] aria-pressed:text-[var(--ef-on-primary)]"
        >
          <span class="material-symbols-outlined text-xl" aria-hidden="true">checklist</span>
          <span class="truncate">{{ t('plans.mobile.errands') }}</span>
          <span class="shrink-0 text-xs">({{ errands().length }})</span>
        </button>
        <button
          type="button"
          aria-controls="mobile-route-panel"
          [attr.aria-pressed]="mobilePanel() === 'route'"
          (click)="showMobilePanel('route')"
          class="flex min-h-12 min-w-0 items-center justify-center gap-2 rounded-lg px-2 py-2 text-sm font-semibold transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--ef-primary)] aria-pressed:bg-[var(--ef-primary)] aria-pressed:text-[var(--ef-on-primary)]"
        >
          <span class="material-symbols-outlined text-xl" aria-hidden="true">map</span>
          <span class="truncate">{{ t('plans.mobile.mapAndRoute') }}</span>
        </button>
      </div>
    </nav>

'''


def patch_editor_html(value: str) -> str:
    value = replace_exact(
        value,
        '    <!-- Main layout -->\n',
        MOBILE_NAV + '    <!-- Main layout -->\n',
        'Mobile navigation insertion',
    )
    value = replace_exact(
        value,
        'class="mx-auto min-h-[calc(100dvh-5rem)] max-w-7xl px-4 py-8 sm:px-6 lg:py-12"',
        'class="mx-auto min-h-[calc(100dvh-5rem)] max-w-7xl px-3 py-5 sm:px-6 sm:py-8 lg:py-12"',
        'Mobile page spacing',
    )
    value = replace_exact(
        value,
        'class="break-words text-3xl font-bold tracking-tight sm:text-4xl"',
        'class="break-words text-2xl font-bold tracking-tight sm:text-4xl"',
        'Mobile plan title',
    )
    value = replace_exact(
        value,
        'class="grid items-start gap-6 lg:grid-cols-[minmax(0,420px)_minmax(0,1fr)]"',
        'class="grid min-w-0 items-start gap-4 lg:grid-cols-[minmax(0,420px)_minmax(0,1fr)] lg:gap-6"',
        'Mobile plan grid',
    )
    value = replace_exact(
        value,
        '      <!-- Errands panel -->\n      <section\n        class="rounded-[var(--ef-radius-card)] border border-[var(--ef-border)] bg-[var(--ef-surface)] p-6"\n        aria-labelledby="errands-heading"',
        '      <!-- Errands panel -->\n      <section\n        id="mobile-errands-panel"\n        [class.hidden]="mobilePanel() !== \'errands\'"\n        class="min-w-0 scroll-mt-36 rounded-[var(--ef-radius-card)] border border-[var(--ef-border)] bg-[var(--ef-surface)] p-3 sm:p-6 lg:block"\n        aria-labelledby="errands-heading"',
        'Errands panel mobile visibility',
    )
    value = replace_exact(
        value,
        '      <!-- Map panel -->\n      <section\n        #mapPanel\n        class="overflow-hidden rounded-[var(--ef-radius-card)] border border-[var(--ef-border)] bg-[var(--ef-surface)]"\n        aria-labelledby="map-heading"',
        '      <!-- Map panel -->\n      <section\n        #mapPanel\n        id="mobile-route-panel"\n        [class.hidden]="mobilePanel() !== \'route\'"\n        class="min-w-0 scroll-mt-36 overflow-hidden rounded-[var(--ef-radius-card)] border border-[var(--ef-border)] bg-[var(--ef-surface)] lg:block"\n        aria-labelledby="map-heading"',
        'Map panel mobile visibility',
    )
    # Improve touch targets without making the existing button labels redundant.
    def icon_touch_class(s: str) -> str:
        if 'h-10 w-10' not in s:
            raise PatchError('Edit/delete touch targets: expected 40px control')
        return s.replace('h-10 w-10', 'h-11 w-11', 1).replace(' px-3 ', ' ', 1)

    value = replace_button_class(value, 'startEditingErrand(errand)', icon_touch_class, 'Edit icon button')
    value = replace_button_class(value, 'askDeleteErrand(errand.id)', icon_touch_class, 'Delete icon button')

    def handle_touch_class(s: str) -> str:
        if 'min-h-10 min-w-10' not in s:
            raise PatchError('Reorder handle: expected 40px control')
        return s.replace('min-h-10 min-w-10', 'min-h-11 min-w-11 touch-none', 1)
    # cdkDragHandle button has no click; locate it by directive within a single button.
    pattern = re.compile(r'<button\b(?:(?!</?button\b).)*?\bcdkDragHandle\b(?:(?!</?button\b).)*?>', re.DOTALL)
    matches = list(pattern.finditer(value))
    if len(matches) != 1:
        raise PatchError(f'Reorder drag handle: expected one, found {len(matches)}')
    m = matches[0]
    original = m.group(0)
    cm = re.search(r'class="([^"]+)"', original)
    if cm is None:
        raise PatchError('Reorder handle has no class')
    modified = original[:cm.start(1)] + handle_touch_class(cm.group(1)) + original[cm.end(1):]
    value = value[:m.start()] + modified + value[m.end():]

    value = replace_exact(
        value,
        '                cdkDrag\n                [cdkDragDisabled]="!canReorderErrands()"',
        '                cdkDrag\n                [cdkDragStartDelay]="{ touch: 200, mouse: 0 }"\n                [cdkDragDisabled]="!canReorderErrands()"',
        'Touch drag delay',
    )
    value = replace_exact(
        value,
        'class="ml-auto flex flex-wrap items-center gap-3"',
        'class="flex w-full flex-wrap items-center justify-between gap-2 sm:ml-auto sm:w-auto sm:gap-3"',
        'Reorder controls narrow layout',
    )
    value = replace_exact(
        value,
        'class="min-h-10 rounded-lg border border-[var(--ef-border)] bg-[var(--ef-surface)] px-2 text-sm text-[var(--ef-text)] transition-colors',
        'class="min-h-11 rounded-lg border border-[var(--ef-border)] bg-[var(--ef-surface)] px-2 text-sm text-[var(--ef-text)] transition-colors',
        'Position selector touch size',
    )
    value = replace_exact(
        value,
        '              (click)="calculateRoute()"\n              class="inline-flex min-h-11',
        '              (click)="calculateRoute()"\n              class="inline-flex w-full sm:w-auto min-h-11',
        'Mobile route action width',
    )
    return value


def patch_editor_ts(value: str) -> str:
    value = replace_exact(
        value,
        "  @ViewChild('mapPanel')\n  private mapPanel?: ElementRef<HTMLElement>;",
        "  @ViewChild('mapPanel')\n  private mapPanel?: ElementRef<HTMLElement>;\n\n  @ViewChild('mobileNavigation')\n  private mobileNavigation?: ElementRef<HTMLElement>;",
        'Mobile navigation ViewChild',
    )
    value = replace_exact(
        value,
        '  protected readonly calculatingRoute = signal(false);',
        "  protected readonly mobilePanel = signal<'errands' | 'route'>('errands');\n\n  protected readonly calculatingRoute = signal(false);",
        'Mobile panel signal',
    )
    value = replace_exact(
        value,
        '  protected beginMapPick(): void {',
        '''  protected showMobilePanel(panel: 'errands' | 'route'): void {
    this.mobilePanel.set(panel);

    if (window.matchMedia('(max-width: 1023px)').matches) {
      requestAnimationFrame(() => {
        this.mobileNavigation?.nativeElement.scrollIntoView({
          behavior: 'smooth',
          block: 'start',
        });
      });
    }
  }

  protected beginMapPick(): void {''',
        'Mobile panel navigation method',
    )
    value = replace_exact(
        value,
        '    this.mapPickTarget.set(null);\n\n    if (target === \'errand\') {\n      this.mapPoint.set(point);',
        "    this.mapPickTarget.set(null);\n    this.mobilePanel.set('errands');\n\n    if (target === 'errand') {\n      this.mapPoint.set(point);",
        'Return to form after selecting a map point',
    )
    value = replace_exact(
        value,
        '''  private scrollToMap(): void {
    requestAnimationFrame(() => {
      this.mapPanel?.nativeElement.scrollIntoView({
        behavior: 'smooth',
        block: 'center',
      });
    });
  }''',
        '''  private scrollToMap(): void {
    this.mobilePanel.set('route');

    requestAnimationFrame(() => {
      this.mapPanel?.nativeElement.scrollIntoView({
        behavior: 'smooth',
        block: 'start',
      });
    });
  }''',
        'Reveal map when choosing a location',
    )
    # Only switch to map after a successful route calculation, not after an API error.
    value = replace_exact(
        value,
        '          this.calculatedRoute.set(result);',
        '''          this.calculatedRoute.set(result);

          if (window.matchMedia('(max-width: 1023px)').matches) {
            this.scrollToMap();
          }''',
        'Reveal calculated route on mobile',
    )
    return value


def patch_map_ts(value: str) -> str:
    value = replace_exact(
        value,
        '  private map?: OlMap;\n',
        '  private map?: OlMap;\n  private resizeObserver?: ResizeObserver;\n  private mapWasVisible = false;\n',
        'Map resize observer fields',
    )
    value = replace_exact(
        value,
        '    this.initializeMap();\n\n    this.renderStops(this.stops());',
        '''    this.initializeMap();

    this.resizeObserver = new ResizeObserver(() => {
      const element = this.mapElement.nativeElement;
      const isVisible = element.clientWidth > 0 && element.clientHeight > 0;

      if (isVisible) {
        this.map?.updateSize();

        if (!this.mapWasVisible) {
          this.fitVisibleMapContent();
        }
      }

      this.mapWasVisible = isVisible;
    });
    this.resizeObserver.observe(this.mapElement.nativeElement);

    this.renderStops(this.stops());''',
        'Map observer registration',
    )
    value = replace_exact(
        value,
        '  ngOnDestroy(): void {\n    this.map?.dispose();\n  }',
        '  ngOnDestroy(): void {\n    this.resizeObserver?.disconnect();\n    this.map?.dispose();\n  }',
        'Map resize observer cleanup',
    )
    value = replace_exact(
        value,
        '  private initializeMap(): void {',
        '''  private fitVisibleMapContent(): void {
    if (!this.map) {
      return;
    }

    const source = this.routeSource.getFeatures().length > 0
      ? this.routeSource
      : this.markerSource;

    if (source.getFeatures().length === 0) {
      return;
    }

    const extent = source.getExtent();

    if (!extent) {
      return;
    }

    this.map.getView().fit(extent, {
      padding: [40, 40, 40, 40],
      maxZoom: 16,
      duration: 0,
    });
  }

  private initializeMap(): void {''',
        'Restore map view after hidden mobile panel',
    )
    return value


def patch_map_html(value: str) -> str:
    return replace_exact(
        value,
        'class="h-[360px] w-full sm:h-[480px] xl:h-[600px]"',
        'class="h-[55dvh] min-h-[280px] max-h-[480px] w-full sm:h-[480px] sm:max-h-none xl:h-[600px]"',
        'Responsive map height',
    )


def patch_comparison_html(value: str) -> str:
    value = replace_exact(
        value,
        'class="border-t border-[var(--ef-border)] px-6 py-6"',
        'class="min-w-0 border-t border-[var(--ef-border)] px-3 py-5 sm:px-6 sm:py-6"',
        'Comparison mobile spacing',
    )
    value = replace_exact(
        value,
        'class="grid gap-4 md:grid-cols-2"',
        'class="grid min-w-0 gap-3 md:grid-cols-2 md:gap-4"',
        'Comparison responsive card grid',
    )
    # Update both comparison cards deliberately, not with a one-occurrence replacer.
    value = replace_exact(
        value,
        'class="rounded-2xl border border-[var(--ef-border)] bg-[var(--ef-surface-raised)] p-5"',
        'class="min-w-0 rounded-2xl border border-[var(--ef-border)] bg-[var(--ef-surface-raised)] p-4 sm:p-5"',
        'Original comparison card',
    )
    value = replace_exact(
        value,
        'class="rounded-2xl border border-[var(--ef-primary)] bg-[var(--ef-surface)] p-5"',
        'class="min-w-0 rounded-2xl border border-[var(--ef-primary)] bg-[var(--ef-surface)] p-4 sm:p-5"',
        'Optimized comparison card',
    )
    return value


TRANSLATIONS = {
    'es': {
        'navigation': 'Secciones del plan',
        'errands': 'Recados',
        'mapAndRoute': 'Mapa y ruta',
    },
    'en': {
        'navigation': 'Plan sections',
        'errands': 'Errands',
        'mapAndRoute': 'Map and route',
    },
}


FILES = {
    'frontend/src/app/features/plans/pages/plan-editor/plan-editor.html': patch_editor_html,
    'frontend/src/app/features/plans/pages/plan-editor/plan-editor.ts': patch_editor_ts,
    'frontend/src/app/shared/map/components/route-map/route-map.ts': patch_map_ts,
    'frontend/src/app/shared/map/components/route-map/route-map.html': patch_map_html,
    'frontend/src/app/features/plans/components/route-comparision/route-comparision.html': patch_comparison_html,
}


def prepare(root: Path) -> tuple[dict[Path, str], bool]:
    changed = {}
    already_applied = []
    for name, patch in FILES.items():
        path = root / name
        if not path.is_file():
            raise PatchError(f'Missing file: {name}')
        original = path.read_text(encoding='utf-8')
        if 'plan-editor.html' in name and '<!-- Mobile panel navigation -->' in original:
            already_applied.append(True)
            continue
        if name.endswith('plan-editor.ts') and "protected readonly mobilePanel = signal<" in original:
            already_applied.append(True)
            continue
        if name.endswith('route-map.ts') and 'private resizeObserver?: ResizeObserver;' in original:
            already_applied.append(True)
            continue
        if name.endswith('route-map.html') and 'h-[55dvh]' in original:
            already_applied.append(True)
            continue
        if name.endswith('route-comparision.html') and 'min-w-0 border-t border-[var(--ef-border)] px-3' in original:
            already_applied.append(True)
            continue
        already_applied.append(False)
        changed[path] = patch(original)

    for lang, entry in TRANSLATIONS.items():
        path = root / f'frontend/public/i18n/{lang}.json'
        if not path.is_file():
            raise PatchError(f'Missing translations: {path}')
        original = path.read_text(encoding='utf-8')
        data = json.loads(original)
        plans = data.get('plans')
        if not isinstance(plans, dict):
            raise PatchError(f'Invalid plans translations: {path}')
        current = plans.get('mobile')
        if current is not None and current != entry:
            raise PatchError(f'Mobile translations already exist but differ: {path}')
        if current is None:
            data['plans']['mobile'] = entry
            changed[path] = json.dumps(data, ensure_ascii=False, indent=2) + '\n'
            already_applied.append(False)
        else:
            already_applied.append(True)

    if any(already_applied) and not all(already_applied):
        raise PatchError('Partial installation detected: some files are patched and others are not. No changes made.')
    return changed, all(already_applied)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=Path('.'), help='ErrandFlow repository root')
    parser.add_argument('--check', action='store_true', help='Verify patches without writing')
    args = parser.parse_args()
    root = args.root.resolve()
    try:
        changes, already = prepare(root)
        if already:
            print('ALREADY APPLIED: mobile UI is installed.')
            return 0
        if args.check:
            print(f'CHECK PASSED: {len(changes)} files are ready to update; no files changed.')
            return 0
        for path, content in changes.items():
            path.write_text(content, encoding='utf-8')
            print('Updated:', path.relative_to(root))
        print('APPLIED: mobile UI changes installed.')
        return 0
    except (PatchError, ValueError, OSError) as error:
        print('ERROR:', error)
        return 1


if __name__ == '__main__':
    raise SystemExit(main())
