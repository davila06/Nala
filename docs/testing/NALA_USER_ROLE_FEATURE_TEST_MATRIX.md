# NALA User Role Feature Test Matrix

**Cutoff:** 2026-09-28  
**Evidence:** `dotnet test PawTrack.sln --no-build --verbosity minimal` passed 1,820 tests; seed chain passed twice on local `PawTrackDev`.

| ID      | User/role        | Scope                   | Plan/feature            | UI/API/DB evidence | Tests                            | Seed                    | Result                        |
| ------- | ---------------- | ----------------------- | ----------------------- | ------------------ | -------------------------------- | ----------------------- | ----------------------------- |
| AUTH-01 | Anonymous        | platform                | admin audit             | API                | AuthorizationTests               | n/a                     | PASS: 401                     |
| AUTH-02 | Anonymous        | public                  | public map              | API                | AuthorizationTests + E2E smoke   | local map seed          | PASS                          |
| AUTH-03 | Owner            | own account/pets        | owner flows             | API/domain/UI      | existing integration/E2E         | owner + pet seeds       | PARTIAL                       |
| AUTH-04 | Clinic           | organization/site       | clinic operations + MFA | API/application    | existing integration             | clinic seeds            | PARTIAL                       |
| AUTH-05 | Admin            | platform administration | plan/admin surfaces     | API                | existing integration             | admin seed              | PARTIAL                       |
| AUTH-06 | SuperAdmin       | platform                | MFA + platform claim    | API                | no dedicated new scenario        | no dedicated seed       | NOT VERIFIED                  |
| TEN-01  | Clinic A/B       | organization/site       | site scope              | API/DB             | existing site integration tests  | clinic data             | PARTIAL/PASS for tested paths |
| TEN-02  | Owner A/B        | owner resources         | pet access              | API/DB             | no complete paired matrix        | partial                 | NOT VERIFIED                  |
| TEN-03  | Provider A/B     | provider scope          | services/bookings       | API/DB             | no dedicated suite               | provider seed           | NOT VERIFIED                  |
| TEN-04  | Municipality A/B | municipal scope         | reports/campaigns       | API/DB             | no dedicated suite               | municipal seed          | NOT VERIFIED                  |
| PLAN-01 | Owner            | account                 | Free/Plus/Familia       | API/application/DB | partial existing tests           | extended seed           | PARTIAL                       |
| PLAN-02 | Any paid role    | account                 | expired subscription    | API/DB             | no complete scenario             | no central expired seed | NOT VERIFIED                  |
| FLAG-01 | Any role         | platform                | feature flag on/off     | API/UI             | no unified flag service verified | no flag seed            | BLOCKED                       |
| E2E-01  | Public user      | public map              | map event rendering     | React/API/DB       | .NET Playwright smoke            | lost-pet seed           | PASS                          |

A row is not considered fully covered merely because a controller, seed or class exists. Full coverage requires the relevant synthetic data, assertion, execution result and persisted-state check.
