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
- two fitted straight candidate lines, one for each offset direction,
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
7. Fit each final guidance line to the target field by snapping the AB endpoints to the target boundary intersections when possible.
8. Allow the user to apply both accepted final guidance lines to the target field, then delete the unneeded one.

## Acceptance rule
The rectified straight line is acceptable only when:
- `minDistance >= rowSpacing - tolerance`, and
- `maxDistance <= rowSpacing + tolerance`.

## Current scope
- Works on ISOXML v3 guidance lines.
- Focuses on offline post-processing, not on-the-fly tractor guidance.
- Uses the target field boundaries to clip or fit AB endpoints.
- Validates using row spacing, then applies machine-width offset as `rowCount × rowSpacing`.
- Generates both machine-width directions so the user can delete the one that is not needed.

## Future refinement
If a straight line is rejected, a later phase may generate a smoothed curve as an intermediate approximation, but that is outside the current scope.
