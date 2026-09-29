# NALA Role Test Matrix

| Role            |        Anonymous denied | Correct role |   Wrong role |       Ownership/tenant |      Plan/expiry | Current evidence                          |
| --------------- | ----------------------: | -----------: | -----------: | ---------------------: | ---------------: | ----------------------------------------- |
| Owner           |                 partial |      partial |      partial |                partial |          partial | integration + frontend E2E                |
| Ally            |                 partial |      partial |      partial |           not verified |     not verified | feature integration tests                 |
| Admin           | PASS for admin endpoint |      partial |      partial | not applicable/partial |          partial | integration + AuthorizationTests baseline |
| Clinic          |                 partial |      partial |      partial |     site scope partial |          partial | clinic integration tests                  |
| Municipality    |                 partial |      partial | not verified |           not verified |     not verified | seed and controller evidence              |
| Store           |                 partial |      partial | not verified |           not verified |     not verified | seed and controller evidence              |
| ServiceProvider |                 partial |      partial | not verified |           not verified |     not verified | seed and controller evidence              |
| Support         |                 partial |      partial | not verified |           not verified |     not verified | role/controller evidence                  |
| SuperAdmin      |                 partial | not verified | not verified |         not applicable | MFA not verified | policy/controller evidence                |

This is a coverage matrix, not a claim that every cell is implemented. `partial` means existing tests or code evidence cover only a subset.
