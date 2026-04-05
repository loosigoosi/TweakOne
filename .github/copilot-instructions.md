# Copilot Instructions

## General Guidelines
- For this project, stay aligned to ISOXML v3 until the app reaches acceptable usability; the UI should show source and target fields side by side with their guidance lines, allow selecting a guidance line to clone, and immediately show it added to the target field.
- For this project, the user workflow must be ZIP-first: the app should accept the exported .zip package containing the TASKDATA folder and return the same packaged .zip structure for reimport.
- For this project, localize existing user-facing strings first using neutral and Italian RESX files before adding more strings later.

## Linee guida del progetto
- For this project's ISOXML import workflow, existing field objects cannot be modified in place on reimport; TASKDATA imports are treated as additions with conflict checks, so prefer generating a new field and replacing the original for experiments. If a new field has boundaries matching an existing field, the system raises a conflict-resolution prompt to choose the primary field; merge behavior is still unknown, so experiments should start with a new field without LSG and then add an imported LSG for reimport testing.
- For this project's import experiments, ISOXML v3 exports may omit the proprietary AGCO JSON file; use TaskData/TestISOXML_v3 as a baseline dataset containing a newly created field with boundaries, farm, and customer.
- For this project's ISOXML v3 experiments, importing a TEST field with a copied A-B `LSG` from another field succeeds; on conflict resolution, choosing the imported field replaces the old one and preserves the added guidance line. The system accepts A-B lines even when they lie outside field boundaries, though translation back inside bounds is desirable.
- For this project's ISOXML guidance workflow, cloned guidance lines may need translation while preserving the A-B direction in parallel; points A and B are used mainly to define direction, while the guidance system generates the parallel lines to follow. The latest automatic translation heuristic placed the cloned line almost entirely inside the target field, with only one endpoint still outside the target boundary.
- For this project's ISOXML workflow, newly arriving Tasks may create new fields instead of reusing the fields referenced by the Task, reinforcing the need to work through generated replacement fields during experiments.
- For this project, a preferable refinement is to let the user vary the offset and reposition both AB points onto intersections with the target field boundary segments, keeping the UI lightweight.
- For the real rectification workflow, the regression-line validation should use the row spacing as the desired offset, then place the final rectified guideline at machine width offset computed as the number of rows × row spacing. Do not auto-choose the final machine-width offset direction yet; generate both offset directions and let the user delete the unneeded one.
- For this project's ISOXML workflow, it should be possible to deliver guideline additions as a TASK update payload instead of importing a replacement field, because in-cab systems can persist added guidelines through TASK import onto the existing field.
- For rectification, when the unacceptable deviation is less than twice the desired tolerance, use a two-pass algorithm that outputs four lines: two outer rectified lines and two inner smoothed lines offset by half the excess deviation.

## Field Previews
- For this project's field previews, guidance lines should be rendered with a 1-pixel stroke thickness, and zoom should react to the mouse wheel, ensuring that guidance lines remain visually 1 pixel thick even while zooming.
- For this project's field previews, use Ctrl+mouse-wheel for zoom because default wheel zoom in Avalonia conflicts with scrolling. Additionally, zoom should be centered on the pointer. Wheel-based zoom should not react when the pointer is over the scrollbars; the zoom anchor must stay centered on the actual viewport content.
- For this project's field previews, the maximum zoom should be 800%.