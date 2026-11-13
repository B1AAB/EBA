# Recorded traverse validation

Implemented the basic-functionality test plan in
`TRAVERSE_BASIC_FUNCTIONALITY_TEST_PLAN.md`. Validation was performed on
2026-11-13 with .NET 10, in both Debug and Release.

## Fixtures and baselines

- Recorded mainnet blocks 71037, 132317, and 717531 using `api_mock_blocks`;
  retained the existing 71036, chain-info, and transaction recordings.
- Generated 44 complete, ordinally sorted TSV tables from recorded responses
  using the Debug source build. A second generation produced identical hashes
  for every table. No baselines were regenerated after the production fixes.
- All final baseline hashes still match. Release update attempts were refused
  for all four heights, without modifying a baseline. Debug update runs failed
  intentionally with "Expected output updated", as required.
- Independently checked transaction counts, input/output sums, fee identities,
  mint/subsidy identities, and script-type count/value profiles against JSON.
  Fees were 0, 0, 5,100,000, and 162,402 satoshi for heights 71036, 71037,
  132317, and 717531 respectively. Minted values were 5,000,000,000 for the
  first three heights and 625,000,000 for 717531. The plan's 71036 and 717531
  row-count anchors all matched.
- The legacy expected-output directory, `EndToEndTests.cs`, `ClientFixture.cs`,
  and `GraphBase.cs` are unchanged by this work.

## Regression evidence

The ten new test methods expand to 25 cases (including an additional
coinbase-only baseline case).

Before the production fixes:

- Ten Debug repetitions: 25/25 cases passed each time.
- Ten Release repetitions: script-profile regression failed in all ten runs;
  the 71036 transaction-feature regression failed in nine runs. No other new
  tests failed. An example first-run diagnostic was `nodes_Tx: 1 missing,
  1 unexpected rows`; script-profile diagnostics reported `nodes_Block:
  1 missing, 1 unexpected rows`.

After atomic profile updates and complete-Tx pre-registration:

| Validation | Result |
|---|---|
| New tests in Visual Studio | 25/25 passed |
| Additional Unit-only repetitions | 10 Debug and 20 Release, 56/56 each |
| Complete in-process test-project repetitions, excluding IntegrationTests | 10 Debug and 20 Release, 163/163 each (4,890 passes) |
| Full solution build | Succeeded |
| Full solution tests, Debug | 181/181 passed after the test-only integration follow-up below |
| Full solution tests, Release | 181/181 passed after the same follow-up |

The complete repeated runs include the existing Functional-category traverse
cancellation, artifact, and resume tests, not only Unit-category tests.

## Sensitivity checks

All mutations were made exclusively in isolated copies under
`%TEMP%/EBA-traverse-mutations`, never in the working source tree. Every
required detector from the plan failed. Reverting either fix was detected on
its first Release attempt.

| Mutation | Failed cases / selected cases | Required tests detected it |
|---|---:|---|
| Omit Fee edge | 6/7 | Expected graph; transaction balances |
| Increment Credits value | 7/7 | Expected graph; transaction balances |
| Skip last transaction output | 6/7 | Expected graph; input/output counts |
| Serialize Time as MedianTime, preserving the header | 4/4 | Expected graph |
| Map script types to Unknown | 7/7 | Expected graph (including Script rows); script profiles |
| Skip final range height | 2/2 | Range graph; block accounting |
| Request each block twice | 1/1 | Request accounting |
| Remove Fee serialization strategy | 4/4 | Expected graph |
| Revert atomic profiles (Release) | 2/3 | Script profiles |
| Revert complete-Tx registration (Release) | 1/3 | Transaction features |

TRX evidence is under `%TEMP%/EBA-traverse-validation`:
`prefix-*.trx`, `postfix-*.trx`, `postfix-complete-*.trx`,
`mutation-*.trx`, `release-update-guard.trx`, and `full-solution-*.trx`.

## Integration configuration follow-up (test-only)

Confirmed the original fixed-port test fails while local Bitcoin Core owns
8332: `HttpListenerException`, "The process cannot access the file because it
is being used by another process." The first free-port version passed
`--client-uri http://localhost:{port}/rest/` to the executable.

That exposed an existing CLI defect: System.CommandLine 2.0.3 cannot convert
that option's string to `System.Uri`. The child exits with
`Cannot parse argument 'http://localhost:{port}/rest/' for option '--client-uri'
as expected type 'System.Uri'` (`CLI.cs`, `Option<Uri>`; `OptionsBinder.Build`).
The initial full-solution runs consequently passed only 180/181 tests in each
configuration, as recorded in the original `full-solution-*.trx` evidence.

The follow-up fixes the test configuration without changing production code:

- `TraverseOptionsBuilder.WithClientUri` supplies the free-port mock URI.
- The test serializes its isolated options to their normal status filename and
  launches the real executable with the supported `--status-filename` option.
  `OptionsBinder` reads this JSON before binding individual options; the range,
  URI, and output paths are all supplied in that configuration.
- `ProcessStartInfo.ArgumentList` handles paths safely; the temporary path
  deliberately includes spaces. The test disposes its child process and
  explicitly asserts exit code zero with stdout/stderr diagnostics.
- Existing graph file and line-count checks remain unchanged. No test is
  skipped and no production source change is required for this follow-up.

Verified the integration test in Visual Studio, then three additional runs
per configuration. Full solution tests now pass **181/181 in both Debug and
Release**, including all 164 cases in `AAB.EBA.Tests`. Production source diff
snapshots remained identical throughout this follow-up. Its TRX evidence is
under `%TEMP%/EBA-integration-test-fix` (`full-*.trx`, `integration-*.trx`).

Direct `--client-uri` conversion remains a separate CLI defect; this test
covers executing a real traverse and its output using supported JSON options,
not that individual argument's parser.

Previously produced Release traversal output is not repaired by these fixes;
correcting its counters and missing Tx features requires a new traversal.
