import { Component, input } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { TranslocoPipe } from '@jsverse/transloco';

export type FormInputType = 'text' | 'date' | 'time';

@Component({
  selector: 'app-form-input',
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInputModule, TranslocoPipe],
  template: `
    <mat-form-field appearance="outline" class="w-full">
      <mat-label>{{ labelKey() | transloco }}</mat-label>

      <input
        matInput
        [type]="type()"
        [formControl]="control()"
        [required]="required()"
        [attr.maxlength]="maxLength()"
      />

      @if (control().touched || control().dirty) {
        @if (control().hasError('required')) {
          <mat-error>{{ 'forms.errors.required' | transloco }}</mat-error>
        } @else if (control().hasError('maxlength')) {
          <mat-error>
            {{ 'forms.errors.maxLength' | transloco: { max: maxLength() } }}
          </mat-error>
        }
      }
    </mat-form-field>
  `,
})
export class FormInputComponent {
  readonly control = input.required<FormControl<string>>();
  readonly labelKey = input.required<string>();
  readonly type = input<FormInputType>('text');
  readonly required = input(false);
  readonly maxLength = input<number | null>(null);
}
