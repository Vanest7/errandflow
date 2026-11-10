import { moveErrandIds } from './reorder-errands';

const errands = [
  { id: 'second', originalPosition: 2 },
  { id: 'first', originalPosition: 1 },
  { id: 'third', originalPosition: 3 },
];

describe('moveErrandIds', () => {
  it('moves a stop upward using its original position', () => {
    expect(moveErrandIds(errands, 'third', -1)).toEqual([
      'first', 'third', 'second',
    ]);
  });

  it('moves a stop downward using its original position', () => {
    expect(moveErrandIds(errands, 'first', 1)).toEqual([
      'second', 'first', 'third',
    ]);
  });

  it('rejects moves beyond the edges and unknown IDs', () => {
    expect(moveErrandIds(errands, 'first', -1)).toBeNull();
    expect(moveErrandIds(errands, 'third', 1)).toBeNull();
    expect(moveErrandIds(errands, 'missing', 1)).toBeNull();
  });

  it('does not change the input list', () => {
    moveErrandIds(errands, 'second', 1);
    expect(errands.map((errand) => errand.id)).toEqual([
      'second', 'first', 'third',
    ]);
  });
});
