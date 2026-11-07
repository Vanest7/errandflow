import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export const trimmedRequiredValidator: ValidatorFn = (
  control: AbstractControl,
): ValidationErrors | null => {
  const value: unknown = control.value;

  if (typeof value !== 'string' || value.trim().length === 0) {
    return { required: true };
  }

  return null;
};
