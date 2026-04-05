# Guideline Rectification

## Goal
Convert a curved guidance line into a straight candidate line that can be reused for a later seeding campaign.

## Problem
When seeding starts from an irregular field boundary, the first AB line is often chosen by eye and does not represent the best long straight pass for the field. The application must analyze a curved guideline exported from the tractor and determine whether a straight replacement line is acceptable.

## Inputs
- A source `LSG` guidance line, typically a polyline.
- A target `PFD` partfield used to fit the straight candidate to the target boundaries.
- A row spacing in meters, for example `0.75`.
- A row count for the implement, for example `4`.
- A tolerance in meters, for example `0.10`.
- An optional designator for the generated straight line.

## Output
A rectification analysis result containing:
- the applied machine-width offset in meters,
- either two fitted straight candidate lines or four two-pass candidate lines,
- whether each candidate is acceptable,
- minimum distance from the source polyline to each candidate analysis line,
- maximum distance from the source polyline to each candidate analysis line,
- maximum absolute deviation from the row spacing.

## Functional behavior
1. Fit a straight regression line to the source polyline.
2. Build two parallel analysis lines at `+rowSpacing` and `-rowSpacing` from the regression line.
3. Evaluate each analysis line against the source polyline.
4. Accept an analysis direction only when every measured distance from the source polyline to that analysis line remains within `rowSpacing ± tolerance`.
5. Compute the applied machine-width offset as `rowCount × rowSpacing`.
6. Build two final guidance lines at `+applicationOffset` and `-applicationOffset` from the regression line.
7. Keep the generated A and B endpoints aligned on the normals of the analyzed regression line so all produced lines share the same longitudinal extents.
8. Allow the user to apply both accepted final guidance lines to the target field, then delete the unneeded one.
9. If a straight candidate is rejected but its excess deviation above the tolerance envelope remains below `2 × tolerance`, generate a two-pass output for that direction:
   1. an outer rectified line at the full application offset,
   2. an inner smoothed line halfway between the source polyline and the outer rectified line.
10. When both directions enter two-pass mode, output four lines total: two inner smoothed lines and two outer rectified lines.

## Acceptance rule
The rectified straight line is acceptable only when:
- `minDistance >= rowSpacing - tolerance`, and
- `maxDistance <= rowSpacing + tolerance`.

## Current scope
- Works on ISOXML v3 guidance lines.
- Focuses on offline post-processing, not on-the-fly tractor guidance.
- Uses the target field only as the destination context for the generated guidance lines; rectification itself does not clip or truncate the generated A-B extents.
- Validates using row spacing, then applies machine-width offset as `rowCount × rowSpacing`.
- Generates both machine-width directions so the user can delete the one that is not needed.
- Uses a two-pass fallback when the excess deviation is positive but still less than `2 × tolerance`.

## Future refinement
If a straight line is rejected, a later phase may generate a smoothed curve as an intermediate approximation, but that is outside the current scope.
