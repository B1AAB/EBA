**This file contains references for extending testing to more comprehensive cases.**

## Recorded traverse tests

`BitcoinOrchestratorTraverseTests`, `BitcoinOrchestratorTraverseConsistencyTests`,
and `BitcoinChainAgentGetGraphTests` replay committed mainnet REST responses in
process. They require no Bitcoin Core, sockets, or child process. The fixtures
cover heights 71036, 71037 (coinbase only), 132317, and 717531.

Run with `dotnet test tests/AAB.EBA.Tests -c Debug --filter "Category=Functional"`
(also run in Release to exercise parallel transaction processing).

Categories: `Unit` tests one component without the orchestrator; `Functional`
runs an in-process traverse through `TraverseRunner`; `Integration` starts the
executable. Run without a filter to cover all three. `EndToEndTests` and
`ClientFixture` remain available for legacy client and transaction-endpoint tests.

To intentionally regenerate the complete sorted TSV baselines, set
`EBA_UPDATE_EXPECTED=1` and run the Debug tests filtered by
`FullyQualifiedName~TraverseAsync_SingleBlock_WritesExpectedGraph`. Update runs
write to `tests/MockData/Bitcoin/ExpectedOutputs/Traverse/{height}` and fail with
"Expected output updated"; Release updates are refused. Unset the variable,
review the diff, and run normally. Do not generate baselines from live-node
traversals: confirmations must match the recordings.

`IntegrationTests` remains separate: it starts the executable with a mock HTTP
server on a free port and compares complete graph contents with the TSV baselines
for heights 71036, 132317, and 717531.
It supplies the range and mock URI through the supported `--status-filename`
JSON configuration. Direct `--client-uri` argument conversion is not covered
by this output integration test.

## How to check the tests still detect defects

Apply each mutation only in an isolated copy, never in the working tree. Run
`BitcoinOrchestratorTraverseTests` and `BitcoinOrchestratorTraverseConsistencyTests`
for golden and independent JSON assertions. Run fix reversions in Release up to
three times: Debug serial processing can mask concurrency defects. Do not update
baselines during mutation checks. A mutation must cause an assertion failure,
not merely a compiler error.

| Mutation | Detector |
|---|---|
| Omit Fee edge | Full golden graph; recorded transaction balances |
| Increment Credits value | Full golden graph; recorded transaction balances |
| Skip last transaction output | Full golden graph; recorded input/output counts |
| Serialize Time as MedianTime, preserving the header | Full golden graph |
| Map script types to Unknown | Full golden graph, including Script and Block columns |
| Skip final range height | Range graph; block accounting |
| Request each block twice | Request accounting |
| Remove Fee serialization strategy | Full golden graph |
| Revert atomic profiles (Release) | Full golden graph's Block columns |
| Revert complete-Tx registration (Release) | Full golden graph's Tx columns |

## Consolidated assertion coverage

Each row lists removed methods (suffixes shown) and the test retaining their
checks. All original cancellation points and recorded heights remain covered.

| Class | Removed method suffixes | Replacement |
|---|---|---|
| Cancellation | `Canceled_ThrowsOperationCanceledException`, `Canceled_AccountsForEveryBlockExactlyOnce`, `Canceled_RegistersNoBlockAsFailed`, `Canceled_WrittenBlocksEqualUncanceledTraverse`, `Canceled_WritesSharedNodesOfEveryWrittenBlock` | `TraverseAsync_Canceled_LeavesConsistentState`; `RunUntilCanceledAsync` checks the exception; shared rows checked whenever any block is written |
| Cancellation | `CanceledBeforeFirstBlock_RequestsNoBlock`, `CanceledBeforeFirstBlock_LeavesEveryBlockPending` | `TraverseAsync_CanceledBeforeFirstBlock_RequestsNothingAndLeavesEveryBlockPending` |
| Cancellation | `CanceledBeforeStart_CreatesNoFiles`, `CanceledBeforeStart_ThrowsOperationCanceledException` | `TraverseAsync_CanceledBeforeStart_ThrowsAndCreatesNoFiles` |
| Resume | `ResumedAfterCancellation_WritesEveryBlockExactlyOnce`, `ResumedAfterCancellation_LeavesNoBlockPending`, `ResumedAfterCancellation_OutputEqualsUncanceledTraverse`, `ResumedAfterCancellation_LeavesEarlierBatchFilesUnchanged` | `TraverseAsync_ResumedAfterCancellation_CompletesTheTraverse` |
| Artifacts | `Canceled_LeavesWellFormedStatusFile`, `Canceled_LeavesReadableBatchFiles`, `Canceled_BatchesListEveryElementType`, `Canceled_ConstantAndSchemaFilesEqualUncanceledTraverse`, `Canceled_ReleasesEveryFile`, `Canceled_LeavesNoUnexpectedFiles` | `TraverseAsync_Canceled_LeavesWellFormedArtifacts` |
| Consistency | `SingleBlock_WritesEveryInputAndOutputOfEveryTransaction`, `SingleBlock_BalancesEveryTransaction`, `SingleBlock_MintsCoinbaseOutputsMinusFees` | `TraverseAsync_SingleBlock_MatchesRecordedBlock`, using all three named assertion helpers |
| GetGraph | `RecordedBlock_BuildsOneEdgePerInputOutputAndTransaction`, `RecordedBlock_AddsCoinbaseTransactionNode`, `RecordedBlock_AddsBlockAndReferencedTransactionNodes` | `GetGraph_RecordedBlock_MatchesRecordedBlock` |
| Golden | `SingleBlock_WritesExpectedScriptTypeProfile`, `SingleBlock_WritesFeaturesOfEveryTransaction` | `TraverseAsync_SingleBlock_WritesExpectedGraph`, now comparing all columns for all four heights |

Cancellation and resume compare full Block and Tx rows with the same double
tolerance as the golden comparisons. Baseline fixtures own separate directories;
tests never resume or modify a shared baseline.

## Improvement validation (2026-11-13)

Measured on .NET 10, Windows. Times are summed case durations from one before
and one rebuilt after run, not benchmarks; concurrent cases overlap in wall time.

| Class | Cases before → after | Debug seconds before → after | Release seconds before → after |
|---|---:|---:|---:|
| Cancellation | 70 → 29 | 87.4 → 11.2 | 93.4 → 13.8 |
| Resume | 21 → 7 | 64.4 → 7.6 | 74.6 → 9.5 |
| Cancellation artifacts | 12 → 2 | 21.8 → 1.1 | 34.8 → 1.2 |
| Recorded golden traverse | 13 → 9 | 8.1 → 3.3 | 10.6 → 3.5 |
| Recorded consistency | 9 → 3 | 4.5 → 1.5 | 5.9 → 1.3 |
| Direct graph | 10 → 4 | 1.1 → 0.8 | 1.7 → 0.7 |
| Batching | 6 → 6 | 7.7 → 3.9 | 9.6 → 4.3 |
| Executable integration | 1 → 3 | 1.2 → 3.0 | 2.1 → 2.8 |
| Persistent graph buffer | 5 → 5 | 1.9 → 2.3 | 3.8 → 2.2 |
| Height mapping | 2 → 2 | 2.2 → 2.3 | 4.3 → 2.2 |
| Agent cancellation | 6 → 6 | 0.1 → 0.1 | 0.1 → 0.1 |
| Persistent object cancellation | 3 → 3 | 1.6 → 1.6 | 1.6 → 1.6 |
| Batch | 7 → 7 | 0.1 → 0.1 | 0.2 → 0.1 |
| Legacy EndToEnd | 4 → 4 | 0.8 → 0.8 | 1.4 → 0.8 |
| Working-directory helper | 4 → 4 | <0.1 → <0.1 | <0.1 → <0.1 |
| Output-reader helper | 0 → 1 | — → 0.7 | — → 0.5 |

Total cases: **173 → 95**, including one new canonical-reader regression.
Wall-clock test durations: approximately **87 → 12 seconds Debug** and
**93 → 15 seconds Release**. All 95 cases passed ten complete Debug repetitions
and twenty complete Release repetitions (2,850 case passes). Full-row
cancellation/resume and recorded parallel/many-batch cases also passed twenty
earlier Release repetitions; the final repetitions include the final mapping fix.

All ten compiling mutations above were detected in an isolated copy. Eight
deterministic mutations failed their first attempt; reverting atomic profiles
failed the first Release attempt (2/4 golden cases), and reverting complete-Tx
registration failed the second Release attempt (1/4 golden cases).
Production source, recorded responses, and all 44 Traverse TSV baselines were
unchanged. The unused legacy `ExpectedOutputs/71036-71036` directory was removed;
transaction recordings and legacy client tests remain available.

## Fee
Transaction fee is paid by the requester; for instance, see:

```json
"txid": "882004308069eab12cfb24708b8121d72b97d755c916aa8900bb99210d7796a7"
```

where input value is `792.8964` and `637.7955` is change 
(returned to the requesters address), `155.1` is paid to 
the destination address, and fee is `0.0009`. In this example:

```json
792.8964 = 637.7955 + 0.0009 + 155.1
```

where after applying the change: 

```json
155.1009 = 0.0009 + 155.1
```


## Address to payment type association

https://bitcoin.stackexchange.com/a/75124/129532


Will all transactions have address associated with them? No!

https://bitcoin.stackexchange.com/a/96866/129532


## Tx

See the following pages for different aspects of transactions for better testing.

- https://developer.bitcoin.org/devguide/transactions.html
- https://developer.bitcoin.org/reference/transactions.html
- https://www.lopp.net/pdf/Bitcoin_Developer_Reference.pdf


## Coinbase Tx

Bitcoins in a coinbase transaction cannot 
be spent until they've received 100 confirmations in the blockchain,
in other words, these coins cannot spent for roughly 16h and 40min. 

More details on the coinbase tx:

- https://www.geeksforgeeks.org/what-is-coinbase-transaction/
- https://academy.bit2me.com/en/what-is-coinbase-transaction/


## Locking scripts
Bitcoin is a programmable currency, and users can lock their 
assets in a number of ways. The Bitcoin community has 
accepted some "standard" locking scripts: 

https://bitcoin.stackexchange.com/a/91090/129532


## Some blocks to test corner cases
 
- `71036`
- `132317`
  - `tx: 7882735f55dfdb9251e6e6b9124e0e0dae44564c5daa9a831fd6ac2b9bd16e8e`
  
     This tx has `0` output and all the input goes to the miner as fee.
	 
	 
## Blocks with more than one output address in the coinbase transaction: 

- Height = 79764
This block has two outputs, no fee.

- Other examples where fee is 0.0: height=79802, height=80661, height=80737
- fee >= 0: height=84482 


See [this](https://bitcoin.stackexchange.com/a/105833/129532) post on 
the usecase of multiple outputs in a coninbase transaction.



- a block with a transaction with fee > 0 (maybe the first block):
2817




## Unit tests to implement:

- Take a block with multiple miner address that has transactions with fee > 0, 
then sum all the values of edges with target set to each miner address. The 
sum should be not be different than the value given in the block json retrieved 
from bitcoin-qt. 

- The sum of all the values of edges where targets are the miner addresses should 
equal sum of the all the fee in all the transactions plus the block mining reward. 

- Building the graph when the whole input is paid as fee (i.e., no output).

# A block that has a lot of chain transfers:

```
120000
```
