# TASK Update Advanced Mode

## Status
This document captures the current product and technical hypothesis for future `TASK update` support.

Current implementation status:
- the app supports the replacement-field workflow
- `TASK update` experiments are suspended
- the current FendtOne portal behavior is not reliable for validating correct field-update semantics

## Current findings

### Confirmed
- ISOXML v3 task import can still be accepted when the unreferenced `TLG00001.bin` file is removed
- the paired `TLG00001.xml` still appears necessary in current experiments
- current FendtOne behavior may accept a task import and still create numbered duplicate fields instead of updating the existing field

### Working assumption
At high confidence, a future successful `TASK update` flow will require machine identity data consistent with the originating terminal or vehicle.

Likely relevant identifiers include:
- `DVC` identifiers
- serial-like controller identifiers
- VIN-like machine identifiers
- terminal-specific metadata
- internal references from `TSK`, `DAN`, `DET`, and related nodes back to the machine profile

## Product direction
The app should support two export modes.

### 1. Standard mode
Audience:
- default users
- agronomic workflow users
- users who want the simplest reliable result

Behavior:
- generate a replacement-field style package
- let the portal resolve field conflict/replacement
- do not require machine-specific task metadata

Rationale:
- easiest workflow
- lowest support burden
- currently the most reliable path

### 2. Advanced mode
Audience:
- power users
- technically confident users
- users willing to provide machine-originated sample data

Behavior:
- generate a `TASK update` package
- reuse machine identity from a real task exported by the user’s own machine
- preserve enough original machine metadata to simulate that machine during task transformation

Rationale:
- likely required to satisfy portal and backend validation rules
- avoids hardcoding AGCO/Fendt-specific machine identity values in the app
- keeps advanced complexity away from the default workflow

## Proposed UX
Add an app configuration page or settings area for advanced users.

Suggested feature name:
- `Machine profile`
- or `Advanced TASK template`

Suggested user action:
- user selects a `.zip` exported from their own machine
- the app extracts machine identity metadata from that package
- the app stores a reusable local machine profile
- future `TASK update` exports can be generated using that profile

Suggested UI wording:
- `Use machine task template`
- `Load machine profile from ZIP`
- `Advanced TASK update mode`

## Proposed machine profile model
The app should not store an entire imported task as the effective runtime object if a smaller reusable profile is enough.

Suggested extracted profile contents:
- source ZIP path for traceability
- ISOXML version
- `DVC` subtree or equivalent machine identity block
- any referenced `DET`, `DOR`, `DPD`, `DPT`, `DVP`, `VPN`, `DAN` nodes required to keep references valid
- terminal/controller identifiers
- serial/VIN-like values
- proprietary external files if still required
- mapping of ids that must remain stable in generated tasks

Suggested local persistence:
- simple JSON metadata plus cached extracted files
- or keep the original ZIP path and re-read on demand

## Export strategy hypothesis
When advanced mode is enabled:
1. load the user-provided machine template
2. extract the minimum valid machine identity graph
3. merge new guideline/task content into a transformed task payload
4. preserve required references and external files
5. emit a ZIP-first package for import

The app should prefer minimum viable payload generation, but only after reference integrity is preserved.

## Non-goals for now
- do not implement this flow yet
- do not assume FendtOne field-update behavior is correct until the portal bug is fixed
- do not remove the replacement-field workflow
- do not attempt generic vendor-neutral task simulation without real-world validation

## Risks
- portal behavior may still be buggy even with correct machine identity
- required machine metadata may include undocumented vendor-specific fields
- external binary or XML artifacts may still influence acceptance indirectly
- ISOXML v3 and v4 may require different identity handling

## Recommended next step when portal behavior is fixed
1. ask the user for a real machine-exported ZIP
2. identify the minimum reusable machine identity subset
3. prototype a `MachineProfile` model
4. add a configuration page for loading and storing that profile
5. generate one experimental `TASK update` package from that profile
6. validate import against the corrected portal

## Branch stop point
This branch intentionally stops at specification level for the advanced `TASK update` mode.

Reason:
- current FendtOne behavior is not a trustworthy validator for the feature
- further implementation would risk optimizing against a portal-side bug instead of the actual protocol requirements
