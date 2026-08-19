---
id: eg
title: Cookbook
description: Bitcoin MCP Prompt Examples
sidebar_label: Cookbook
---



Prompts you can run against the Bitcoin MCP server.

Placeholders like `<TXID>`, `<ADDRESS>`, `<SHA256>`, and `<HEIGHT>` are meant to be swapped out with real values before running.

---

## Block Tools

### `GetLatestBlock`

- prompt: "What's the database cutoff point right now?"
-- Example Response: Latest block height in the service's database is 800,000.


### `GetBlockInfo`
- prompt: "Show me the transaction count and median time for block 800,000."
-- Example Response: 
    Block 800,000:

    Transaction count: 3,721
    Median time: 1690165851 (July 24, 2023)


- prompt: "What was the market cap and NUPL at block 800,000?"
-- Example Response: 
    Block 800,000 (July 24, 2023):

    Market cap: ~$583.57 billion
    NUPL (Net Unrealized Profit/Loss): -0.0158

    That NUPL value is just barely negative — right around the "hope/fear" transition zone, meaning the market was roughly at breakeven between unrealized profit and loss at that point.


- prompt: "How many coins were minted in block 800,000?"
-- Example Response: 
    625,000,000 sats minted in block 800,000 — that's 6.25 BTC, the standard block reward after the 2020 halving (and still before the 2024 halving dropped it to 3.125).

- prompt: "Give me the OHLCV, MVRV, total supply and burned coins for block 800,000."
-- Example Response:

    Block 800,000 (July 24, 2023):

    OHLCV: Open/High/Low/Close all $30,023, Volume 5,396,267, VWAP $30,023 — a flat candle, likely representing a single reference price point rather than intra-block trading range
    MVRV: 0.984 — just under 1, meaning market value was slightly below realized value (coins were on average worth marginally less than what holders paid, consistent with the near-zero NUPL from before)
    Total supply: 19,437,436.85 BTC (in sats: 1,943,743,685,119,702)
    Burned coins: 1 sat

---



## 2. Script / Address Tools (`BitcoinScriptTools`)

### `GetScript`
- prompt: "Look up this Bitcoin script hash and tell me what type of script it is and what's the current balance: `CNuDXVL4AFEohDSe4SvzTZxhSGFdWThhbwHrgatX6GoT`"
-- eample response: 

    This script is a Taproot output (witness_v1_taproot — a native SegWit v1 / P2TR script, the type introduced by BIP 341).

    Current balance: 4,833,275 satoshis ≈ 0.04833275 BTC


- prompt: "What was the balance of script with SHA256 `CNuDXVL4AFEohDSe4SvzTZxhSGFdWThhbwHrgatX6GoT` at block `795000`?"
-- example response: 
    At block 795000, the balance was 3,467,314 sats ≈ 0.03467314 BTC.




### `GetScriptTxInfo`
- prompt: "Give me transaction summary stats for this script with SHA256 `CNuDXVL4AFEohDSe4SvzTZxhSGFdWThhbwHrgatX6GoT`"

-- example response: 

    Here's the transaction summary for this Taproot script:

    | Stat | Value |
    |---|---|
    | **Total transactions** | 10,047 |
    | **Total received** | 5,094,263 sats (≈0.05094263 BTC) |
    | **Total sent** | 260,988 sats (≈0.00260988 BTC) |
    | **First received** | Block 791,435 — 380 sats |
    | **First sent** | Block 791,572 — 546 sats |
    | **Last received** | Block 800,000 — 330 sats |
    | **Last sent** | Block 795,050 — 546 sats |

    A few things stand out:

    - The very high transaction count (10,047) combined with tiny per-transaction values (many in the hundreds of sats) is a strong signature of an **inscription/ordinals-style script** — this pattern is typical of Taproot outputs used for Bitcoin Ordinals or BRC-20 activity, which took off right around block 791,000+ (early 2023).
    - This confirms your earlier finding: the script received its first funds at block 791,435 — right in the window between your 790,000 (balance 0) and 795,000 (balance 3,467,314) checks.
    - Net balance from these totals (5,094,263 − 260,988 = 4,833,275) matches the current balance exactly, confirming consistency across the data.


### `GetScriptNeighbors`
- prompt: "Show me the last 10 UTXOs the address with SHA `CNuDXVL4AFEohDSe4SvzTZxhSGFdWThhbwHrgatX6GoT` spent and the last 10 it received."

-- example response: 

    **Last 9 UTXOs spent (redeemed), all 546 sats each:**

    | Txid | Height |
    |---|---|
    | `96dca6354968d8f10623167ffce5148150f6c86cdb6e651390e2768d5deb488e` | 798461 |
    | `05cc79d6f7c2f00547f6e8fbb6c7a46eb2ceff48580a917d84769d0bb9588465` | 798301 |
    | `b332dc7b4f28bb01e404d4396b16d8cf0b474f064eadceed400f96da606b59d6` | 797761 |
    | `b0418952f7a4a7ab61cca13bc839f3d4c9ade672bcbf71a6c844e44220ac80b0` | 797746 |
    | `12871c4918f03c644a2d617e015f6742f40f5e9b77bd46034e44b17b977c5406` | 797452 |
    | `cefc7b75ee5a43872d781fc115dfafd6b09cee61cb1cc8425c83af2b96792e41` | 797043 |
    | `972666f7b2e470743e2b5332fb6f486a451f7b1a9b71078249aa8e92ae2fb304` | 796097 |
    | `7d26758c00269a99471520f767e2ebc8d23f05ce1a75661b8a407d0af9f3d38a` | 795918 |
    | `a624094d5ebdee78a86a6e9476191fc1692b47a41fcdd50a72418c872646d503` | 795415 |

    **Last 9 UTXOs received (rewarded), all 546 sats each:**

    | Txid | Height |
    |---|---|
    | `4db9616f60b5ebb1cf03f124a567541d9ecf3824a1f161530e971fc8ad0d54c4` | 795276 |
    | `3d7f34e3bb81d4a542479eb9c897bd1a46d9c1fe6aa1dd01b46342f63e9bb3a5` | 795050 |
    | `b579ae4752f02e52d03b63b972cca6e8d8d648f951217ce21bd66f0d34cdfc21` | 794415 |
    | `ee66c8eec894ebffbb56a9d4ced38bdfeb3b0d32de0cfce7b107d86020a1c905` | 794148 |
    | `a572e24b272b2071519d0515952a85b0131399acfd4e1dc331fe5140364e0a71` | 792074 |
    | `6aae556f1b41ac531bdcabaea9c26b332b46a1e367d087b3f4d08c6b530d7519` | 792074 |
    | `f4f277e8005f48b32eae9e619b5f6415ee153ee1beda1f56cc9afc23446d4720` | 792765 |
    | `5207e8fe3cdd4be4f5a7acb0005efcabc9fff066c556d9dd0f1cb0db0fec820c` | 792067 |
    | `4ff58dc77a90745e9aad126db7a1dd9a013bd60524305b7d8efb127b8de7f3d1` | 792171 |

    The uniform 546-sat values across nearly all entries reinforce the earlier read that this is likely an ordinals/inscription-carrying script — 546 sats is the standard dust-limit output size commonly used for inscription transfers.

---

## 3. Transaction Tools (`BitcoinTxTools`)

### `GetTxSummary`
- prompt: "Summarize transaction 96dca6354968d8f10623167ffce5148150f6c86cdb6e651390e2768d5deb488e — fee, input/output value, script counts, coin age."
-- example response:

    Transaction `96dca6354968d8f10623167ffce5148150f6c86cdb6e651390e2768d5deb488e`:

    | Stat | Value |
    |---|---|
    | **Block height** | 798,461 |
    | **Fee** | 4,618 sats |
    | **Input value (sum spent)** | 38,749 sats |
    | **Output value (sum created)** | 34,131 sats |
    | **Total input scripts** | 3 |
    | **Unique input scripts** | 2 |
    | **Total output scripts** | 4 |
    | **Unique output scripts** | 3 |
    | **Min input age (coin dormancy)** | 0 blocks |
    | **Max output age** | 3,411 blocks |
    | **Output value spent so far** | 0 sats |

    Input and output values reconcile with the fee (38,749 − 34,131 = 4,618). The transaction has 3 inputs drawing from only 2 unique scripts (one script address is reused across two inputs), and creates 4 outputs across 3 unique scripts. None of the outputs have been spent yet as of this dataset's cutoff.




- prompt: "Did any of the following transactions spend any coinbase (freshly minted) coins? 96dca6354968d8f10623167ffce5148150f6c86cdb6e651390e2768d5deb488e or 398d2fc88f26471d97558c1bda3ef03921cd0f5b91a6dfb3c1deea69999f6712"

-- example response: 
    **`96dca6354968d8f10623167ffce5148150f6c86cdb6e651390e2768d5deb488e`** — Height 798,461. `SumOfUTxOOfCoinbaseOutputSpentInTx: 0` → **No coinbase coins spent.**

    **`398d2fc88f26471d97558c1bda3ef03921cd0f5b91a6dfb3c1deea69999f6712`** — Height 749,611. `SumOfUTxOOfCoinbaseOutputSpentInTx: 627,625,298` sats → **Yes, this transaction spent coinbase (freshly minted) coins** — about 6.276 BTC of its total input value came from a coinbase output.

    **Summary:** Only the second transaction (`398d2fc8...`) spent coinbase coins; the first (`96dca635...`) did not.


### `GetTxNeighbors`
- prompt: "Show me the inputs and outputs of transaction `398d2fc88f26471d97558c1bda3ef03921cd0f5b91a6dfb3c1deea69999f6712`, with addresses and values, and tell me which outputs of this trasnaction have already been spent?"

-- example response:

    Here's the breakdown for transaction `398d2fc88f26471d97558c1bda3ef03921cd0f5b91a6dfb3c1deea69999f6712` (block 749,611):

    **Inputs (UTXOs spent):**

    | Address | Value (sats) | Created In (Txid, vout) | Created At Height | Age at Spending |
    |---|---|---|---|---|
    | `322eWk59mgQReBbgd5GsUb6iJNJhasakn9` | 4,149,596,200 | `6a0da3415583f315a55a5350a9c4a05cebd9d2245dafbcf99f5716150a3831de`, vout 61 | 749,604 | 7 blocks |
    | `3E73ngbypDNGN2tsg55VBePr1FA8qNDgn8` | 798,710,500 | `d3ccfa27de2ba4816035b356e1caec62cd09abfa4a0eb6d123eba8188c29b70c`, vout 0 | 749,602 | 9 blocks |
    | `3KZDwmJHB6QJ13QPXHaW7SS3yTESFPZoxb` | 627,625,298 | `723abb246d7bc5e94f8885b9749d3cfe479e0f3ddff63cf68150f273a9d3978b`, vout 0 | 749,507 | 104 blocks |

    **Outputs (UTXOs created):**

    | Address | Value (sats) | Created At Height | Spent? | Spent At Height |
    |---|---|---|---|---|
    | `3MqUP6G1daVS5YTD8fz3QgwjZortWwxXFd` | 4,528,083,181 | 749,611 | ✅ Yes | 749,752 |
    | `1CYp3YKivN86H1mVoAajfWiWTd6PrkRUiC` | 8,268,775,718 | 749,611 | ✅ Yes | 753,145 |

    **Which outputs have been spent:** **Both outputs have already been spent** —
    - Output to `3MqUP6G1...` was spent at block 749,752
    - Output to `1CYp3YKi...` was spent at block 753,145

    This matches the earlier summary's `OutputValueSpent: 12,796,858,899` sats, which equals the full sum of both outputs — confirming 100% of this transaction's outputs have been consumed downstream.

- prompt: "What's the age of each input UTXO spent in transaction `398d2fc88f26471d97558c1bda3ef03921cd0f5b91a6dfb3c1deea69999f6712`?"

-- example response: 

    Based on the data already pulled for this transaction, here's the age of each input UTXO:

    | Address | Value (sats) | Created At Height | Spent At Height (749,611) | Age at Spending |
    |---|---|---|---|---|
    | `322eWk59mgQReBbgd5GsUb6iJNJhasakn9` | 4,149,596,200 | 749,604 | 749,611 | **7 blocks** |
    | `3E73ngbypDNGN2tsg55VBePr1FA8qNDgn8` | 798,710,500 | 749,602 | 749,611 | **9 blocks** |
    | `3KZDwmJHB6QJ13QPXHaW7SS3yTESFPZoxb` | 627,625,298 (coinbase) | 749,507 | 749,611 | **104 blocks** |

    All three inputs are relatively young — the oldest (the coinbase input) had only matured 104 blocks (~17 hours) before being spent, and the other two were spent within 7-9 blocks of being created.



---

## 4. Practical / Multi-Tool Prompts

- **UTXO age & coinbase check**
  prompot: "Is the input UTXO in transaction `398d2fc88f26471d97558c1bda3ef03921cd0f5b91a6dfb3c1deea69999f6712` a coinbase reward, and how old is it?"
  -- example response: 

      One of the three inputs is a coinbase reward:

      Coinbase input:

      Address: 3KZDwmJHB6QJ13QPXHaW7SS3yTESFPZoxb
      Value: 627,625,298 sats (≈6.276 BTC)
      Created at height: 749,507
      Spent at height: 749,611
      Age: 104 blocks (~17 hours) at time of spending

      This matches the SumOfUTxOOfCoinbaseOutputSpentInTx value (627,625,298) from the transaction summary, confirming this specific UTXO is the freshly-minted coinbase coin being spent in this transaction.

      The other two inputs (4,149,596,200 sats at 7 blocks old, and 798,710,500 sats at 9 blocks old) are not coinbase — they're ordinary previously-circulated coins.


- **Trace funds back one hop**
  prompt: "Where did the coins currently held by script with sha256 B5smJ9drvUuHjd1wVbQGCaTGeez29ZL2nbLb2VJNSs5R come from?"

  -- example response:

      The script with hash `B5smJ9drvUuHjd1wVbQGCaTGeez29ZL2nbLb2VJNSs5R` (address `bc1px4hurvmluuakya3gqevvqdmggcn8ez2rdkht7mggguku3lyzamssuvvy74`, a taproot script) currently holds a **balance of 0 BTC** — every satoshi it ever received has since been spent.

      Here's the full picture of where its coins came from and went:

      **Origin (funding):**
      - It received a single deposit of **38,691 sats** at block height **799,999**, from transaction `32250e984c...e413d0`.
      - That funding transaction had **one input** — 664,200 sats from address `bc1pa7zun82qw93m7k5306zvm2swcy9rrr09zlmzkv6kvheqxjrh504qk33au6` — which was split across **17 outputs** (a batch/fan-out transaction). Your script was just one of ~9 recipients that each got 38,691 sats in that batch (one recipient, `bc1qrcg5...`, got a larger 40,000-sat share, and another was spent same-block).

      **Disposition (spending):**
      - All 38,691 sats were later spent at block height **800,000**, across **nine separate transactions**, each redeeming 1,269 sats at a time (totaling 11,421 sats accounted for in what was returned) — this pattern (many small, equal-value spends from one deposit) is typical of an inscription/ordinal-style reveal or a similar batch protocol rather than a simple payment.

      So in short: the coins came from a 17-way fan-out transaction funded by a single ~664k-sat input, and the script has since fully spent its balance down to zero, mostly via nine small, uniform-value transactions in the very next block.
        

- **Follow the money forward**
  prompt: "Where did the output of transaction 0a84852aab6dadaf402a417c228a1de34348db2f1849c7837cd5b5718390e01c go next?"

  -- example response: 

    This transaction created **9 outputs**, all worth **88,815 sats** each (at block height 792,765), and all of them have already been spent:

    | Output address | Value | Spent at height |
    |---|---|---|
    | `bc1pueta9xuwq6m766nnyk3j9pg8nzhjlklpactadhxcxz2gw5aawf4ss84k2c` | 88,815 | 792766 |
    | `bc1ps9r77qy8yddpljf4hukrnuc8ltngrynszk0k5jk8aarg0cengarsjavury` | 88,815 | 792766 |
    | `bc1punz8szzd4a5dju9mzg8ed748ysr86u0mm6vd6gxte4kjp8jpuujquvqgmg` | 88,815 | 792766 |
    | `bc1pe6jkxhnk8cdn852py9y9lkgk4g5f7etv6en2uswq5ejdjxk955fqnvplar` | 88,815 | 792766 |
    | `bc1pt5xnt66hehkj7txpk3l5ksle6jercgpywntswk7aejug29v4tadqe65wxy` | 88,815 | 792766 |
    | `bc1p6hc92mpc9e5a4swkhcdmdrzq7pn9xer3hkg7qlw5tacrcy2la9dq69qf27` | 88,815 | **792767** |
    | `bc1pq3f04lhr8yhapluguzezjyd225njnd48kul44k7fwyz7wrhcvn5s0t569a` | 88,815 | 792766 |
    | `bc1pazwj2zq5tz4gk49kgfaex0lveu8tmarw45jtaz5qpjhmrnl92ptqpz2ky3` | 88,815 | 792766 |
    | `bc1pqxxkme5c998td9ftzdwg9ec77jvxwcsz36m63v4nn28xs3gweljq4ksrxp` | 88,815 | 792766 |

    So this looks like a fan-out/consolidation-style split — one input of 2,211,100 sats broken into 9 near-equal outputs — and all 9 were spent within a block or two, almost all in the very next block (792766). 




- **Historical context for an address**
  prompot: "What was Bitcoin's price and the address with SHA256 3CTujXStH8J5Q87v4NqicBqNJgpbcr7U4EpfCQCfecWM balance around block 792485?"
  
  -- example response: 

      At block 792485 (median time ≈ June 2, 2023):

      **Bitcoin price:** trading around **$26,789–$26,821**, closing near **$26,821** (volume ≈21,379,068 for the interval; VWAP ≈$26,802.69).

      **Address balance** (script hash `3CTujXStH8J5Q87v4NqicBqNJgpbcr7U4EpfCQCfecWM`, a P2SH-type script): **12,721,326 satoshis ≈ 0.1272 BTC**, worth roughly **$3,410** at that block's price.



- **Full transaction profile**
  prompt: "Give me a full profile of transaction c92d733660abcaec1e40af4afd69fbee3658f2785798c6ef028ca30cef7eb322: fee, coin age, and where the money came from and went."

  -- example response: 


      Here's the full profile for transaction `c92d733660abcaec1e40af4afd69fbee3658f2785798c6ef028ca30cef7eb322`:

      ## Overview
      - **Confirmed in block:** 792,765 (≈ June 4, 2023)
      - **Fee paid:** 8,950 sats
      - **Total input value:** 2,284,055 sats
      - **Total output value:** 2,275,105 sats (difference = the fee)
      - **Inputs:** 3 UTXOs, all from the same address
      - **Outputs:** 2 UTXOs, to two different addresses

      ## Where the money came from (inputs)
      All three inputs were spent from the same P2SH address, `3GtMJEfsWqvWeYqf9ePYL3ANyWP5a2bBDf`:

      | Value (sats) | Origin tx | Created at height | Coin age at spend |
      |---|---|---|---|
      | 896,000 | `253f8e5a...9a427` (vout 2) | 792,351 | 414 blocks (~2.9 days) |
      | 478,055 | `b6415aae...2be77` (vout 1) | 792,485 | 280 blocks (~1.9 days) |
      | 910,000 | `20f5c28e...58257` (vout 2) | 792,484 | 281 blocks (~1.95 days) |

      So this transaction consolidated three separate UTXOs sitting at the same address, ranging from about 2 to 3 days old (roughly 280–414 blocks), into a single spend.

      ## Where the money went (outputs)
      | Value (sats) | Destination address | Status |
      |---|---|---|
      | 64,005 | `3GtMJEfsWqvWeYqf9ePYL3ANyWP5a2bBDf` (same input address — change) | Spent later, at height 794,273 |
      | 2,211,100 | `bc1p45d7n8m7xyegawusx80memzfjgjn7fk7xfmjehpk56xn8t5ung4s7qegtq` (Taproot/P2TR) | Spent immediately, same block (792,765) |

      ## Interpretation
      This looks like a **consolidation-and-transfer** transaction: three UTXOs held at one P2SH address were combined and sent out mostly to a new Taproot address (bc1p...), with a small change amount (64,005 sats) returned to the original P2SH address. The bulk of the funds (2,211,100 sats) moved on almost immediately in the very same block it was created — suggesting it was quickly forwarded again rather than held.

  


- **Price/market cap at confirmation time**
  prompt: "What was Bitcoin's price and market cap when transaction c92d733660abcaec1e40af4afd69fbee3658f2785798c6ef028ca30cef7eb322 confirmed?"
  -- example response: 

      Transaction `c92d7336...ef7eb322` confirmed in block **792765**, mined around **June 4, 2023, 04:21 UTC**.

      At that block:

      - **Bitcoin price:** ~$27,064–$27,073, closing around **$27,071** (VWAP ≈ $27,069)
      - **Market cap:** ≈ **$524.9 billion**


- **Dormant vs. fresh coin movement**
  prompt: "Did the large payment in transaction c92d733660abcaec1e40af4afd69fbee3658f2785798c6ef028ca30cef7eb322 come from old, dormant coins or freshly moved ones?"
  
  -- example response: 

      All three inputs were consolidated from the same address (3GtMJEfsWqvWeYqf9ePYL3ANyWP5a2bBDf), and none of them were old coins:

      - Input ages at spending: **280, 281, and 414 blocks** — roughly **2 to 3 days** old (assuming ~10 min/block).
      - The `MinInputAge` across the whole transaction was 280 blocks, confirming the coins were all recently created and moved quickly.

      So the large output (2,211,100 sats, ~0.0221 BTC, sent to a Taproot address `bc1p45d...`) was funded by freshly moved coins, not dormant/old ones. The remaining small output (64,005 sats) went back to the same source address as change, and it too was spent quickly (94 blocks later, at height 794273).

      **Bottom line:** this transaction consolidated three UTXOs that were each only a few days old and forwarded most of the value onward — there's no sign of long-dormant coins being reactivated here.


- **Unspent output / possible whale wallet**
  prompt: "Is the output of transaction c92d733660abcaec1e40af4afd69fbee3658f2785798c6ef028ca30cef7eb322 still unspent, and what's the current balance of that address?"
  -- example response: 

      Transaction `c92d7336...` created two outputs, and **both are already spent** — neither is currently unspent:

      | Output | Address | Value (sats) | Created @ height | Spent @ height |
      |---|---|---|---|---|
      | vout 0 | `3GtMJEfsWqvWeYqf9ePYL3ANyWP5a2bBDf` | 64,005 | 792,765 | 794,273 |
      | vout 1 | `bc1p45d7n8m7xyegawusx80memzfjgjn7fk7xfmjehpk56xn8t5ung4s7qegtq` | 2,211,100 | 792,765 | 792,765 (same block — spent immediately) |

      Current balances of those two addresses (as of the latest indexed block):
      - **`3GtMJEfsWqvWeYqf9ePYL3ANyWP5a2bBDf`**: 13,492,546 sats (~0.1349 BTC) — this address has ongoing activity beyond just this tx.
      - **`bc1p45d7n8m7xyegawusx80memzfjgjn7fk7xfmjehpk56xn8t5ung4s7qegtq`**: 0 sats — fully spent out, nothing currently held there.

      If you meant a specific vout (0 or 1) let me know and I can dig further into what spent it.
  

- **UTXO churn pattern for a script**
  prompt: "How has script with SHA256 iNNhp45nQR4aoodwbGZQPZrYA5EhCHKbXQ8JP7zy6UQ UTXO churn looked over time?"
  -- example response: 

      This script's UTXO activity is extremely concentrated — it's not really "churn over time" so much as one burst of activity.

      **Timeline:**
      - **Block 792765**: The script receives a single UTXO worth 88,815 sats (this is essentially its only "real" inbound funding).
      - **Block 792766** (the very next block): That 88,815-sat UTXO is spent in transaction `472cedfa...`, which fans it out into **24 nearly-identical outputs of 2,856 sats each**, all still belonging to this same script.
      - **Same block (792766)**, almost immediately: 23 of those 24 new 2,856-sat outputs are each redeemed individually in 23 separate transactions.
      - **Block 792767** (one block later): The last remaining 2,856-sat output is finally spent.

      **Summary stats:**
      - Total transactions touching this script: 50
      - Total received: 157,359 sats — total sent: 157,359 sats (fully churned, nothing left unspent)
      - Active lifespan: just 2 blocks (792765 → 792767), roughly 10–20 minutes of Bitcoin time

      **Pattern interpretation:** This looks like a classic **fan-out-then-drain** pattern — a single funding UTXO is split into many equal-sized "change" outputs in one transaction, and each of those is spent again almost immediately, all within the same block or the very next one. This is typical of mixing/consolidation services, batch payment tools, or automated wallet software distributing funds, rather than a script that sees gradual, sustained use over time. There's no long-tail activity — it's a single concentrated burst.




