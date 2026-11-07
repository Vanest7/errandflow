import { FormControl } from '@angular/forms';
import { describe, expect, it } from 'vitest';
import { trimmedRequiredValidator } from './trimmed-required.validator';

describe('trimmedRequiredValidator', () => {
  it('rejects empty and whitespace-only values', () => {
    expect(trimmedRequiredValidator(new FormControl(''))).toEqual({ required: true });
    expect(trimmedRequiredValidator(new FormControl('   '))).toEqual({ required: true });
  });

  it('accepts non-empty text', () => {
    expect(trimmedRequiredValidator(new FormControl(' Weekend errands '))).toBeNull();
  });
});
