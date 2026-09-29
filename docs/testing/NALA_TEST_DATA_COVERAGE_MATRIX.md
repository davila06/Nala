# NALA Test Data Coverage Matrix

**Status:** baseline; rows are not marked covered without executable assertions and a recorded run.

| Scenario                              | Seed/source                                      | Executable test                                  | Status       |
| ------------------------------------- | ------------------------------------------------ | ------------------------------------------------ | ------------ |
| Owner with pet                        | extended SQL seed                                | existing integration/E2E subsets                 | partial      |
| Owner without pet                     | not centrally catalogued                         | not verified                                     | blocked      |
| Free/Plus/Familia subscriptions       | extended SQL seed and subscription fixtures      | partial                                          | partial      |
| Clinic with patient and second clinic | clinic seeds/fixtures                            | site tests exist; full patient isolation pending | partial      |
| Two organizations/sites               | integration fixtures                             | organization site tests exist                    | partial      |
| Shelter/refuge pair                   | ally/adoption seeds                              | no dedicated cross-shelter suite                 | not verified |
| Municipality pair                     | enterprise/demo seeds                            | no dedicated cross-municipality suite            | not verified |
| Provider pair                         | extended seed                                    | no dedicated cross-provider suite                | not verified |
| Suspended user                        | no central deterministic scenario manifest found | not verified                                     | blocked      |
| Expired subscription                  | subscription fixtures/data                       | not verified end-to-end                          | blocked      |
| Feature enabled/disabled              | no unified feature flag seed verified            | not verified                                     | blocked      |
| MFA-enabled SuperAdmin                | auth fixtures/domain support                     | not verified as an API matrix                    | blocked      |

The matrix is deliberately conservative: a seed file or class name alone is not coverage evidence.
