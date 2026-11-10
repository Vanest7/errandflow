import type { ErrandDto } from '../../../core/api/generated';

export function moveErrandIds(
  errands: readonly Pick<ErrandDto, 'id' | 'originalPosition'>[],
  errandId: string,
  direction: -1 | 1,
): string[] | null {
  const orderedIds = [...errands]
    .sort((a, b) => Number(a.originalPosition) - Number(b.originalPosition))
    .map((errand) => errand.id);

  const index = orderedIds.indexOf(errandId);
  const targetIndex = index + direction;

  if (index < 0 || targetIndex < 0 || targetIndex >= orderedIds.length) {
    return null;
  }

  [orderedIds[index], orderedIds[targetIndex]] = [
    orderedIds[targetIndex],
    orderedIds[index],
  ];

  return orderedIds;
}
