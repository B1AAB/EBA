---
title: Augmentation
description: Step 3. Augment graph using off-chain data
sidebar_label: Augmentation
sidebar_position: 3
slug: /bitcoin/etl/augment
---

EBA currently supports adding market data to the Bitcoin graph. 
This enriches the graph with market-related features that enable quantitative analysis.


### Step 1: Get Market Data

You may download Bitcoin market data from any source you prefer. 
We recommend the following publicly accessible source:

```
https://www.kaggle.com/datasets/mczielinski/bitcoin-historical-data
```

### Step 2: Map Market Data to Blocks

Bitcoin targets an average block interval of about 10 minutes 
(check [this paper](https://arxiv.org/abs/2510.20028) for detailed plots). 
For this step, EBA uses each block's median time (`mediantime`) 
to align market data with blocks.
Because market data is typically sampled at shorter intervals 
and is not synchronized with block times, 
EBA maps candles to each block using the time window between 
two consecutive blocks' median times.
For each block, candles with timestamps in that interval are 
used to compute aggregated OHLCV values.

You may run the following command for this step.

```shell
.\aab.eba.exe bitcoin map-market --ohlcv-source-filename btcusd_1-min_data.csv --block-market-output-filename mapped-block-ohlcv.tsv
```

### Step 3: Augment the Graph

Run the following command to add market-related features to the graph.

```shell
.\aab.eba.exe bitcoin augment --ohlcv-filename mapped-block-ohlcv.tsv --batches-filename batches.json
```

```shell
mkdir -p pre_augment

find . -maxdepth 1 -type f -name "[0-9]*_nodes_Block.csv.gz" -print0 \
    | xargs -0 -I {} mv {} pre_augment/ 2>/dev/null

find . -maxdepth 1 -type f -name "[0-9]*_nodes_Block_with_economic_indicators.csv.gz" -print0 \
    | while IFS= read -r -d '' f; do
        mv "$f" "${f/_with_economic_indicators/}"
    done
```
