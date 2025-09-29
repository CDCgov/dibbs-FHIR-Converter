# PULL REQUEST

_PR title: Remember to name your PR descriptively and follow [conventional commits](https://cheatsheets.zip/conventional-commits)!_

## Summary

What changes are being proposed?

## Related Issue

Fixes #

## Acceptance Criteria

Please copy the acceptance criteria from your ticket and paste it here for your reviewer(s)

## Additional Information

Anything else the review team should know?

## Checklist

- [ ] ⚠️ Create an associated `dibbs-ecr-viewer` PR & checked that things work on the front-end.
- [ ] If necessary, update any test fixtures/bundles to reflect FHIR conversion changes (in this repo and/or `dibbs-ecr-viewer`)
- [ ] If this code affects the other scrum team, have they been notified? (In Slack, as reviewers, etc.)

⚠️ Do not merge this PR until the associated `dibbs-ecr-viewer` PR is created and validated. When both have been approved:
1. Merge the FHIR converter PR
2. Cut a new release of `dibbs-fhir-converter`
3. Update the [fhir-converter Dockerfile](https://github.com/CDCgov/dibbs-ecr-viewer/blob/main/containers/fhir-converter/Dockerfile) in `dibbs-ecr-viewer` with the updated release branch number.