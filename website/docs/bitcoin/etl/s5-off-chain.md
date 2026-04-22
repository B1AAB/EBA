---
title: Augmentation
description: Step 5. Augment graph using off-chain data
sidebar_label: Augmentation
sidebar_position: 4
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
.\eba.exe bitcoin map-market --ohlcv-source-filename btcusd_1-min_data.csv --block-market-output-filename mapped-block-ohlcv.tsv
```

### Step 3: Augment the Graph

Run the following command to add market-related features to the graph.

```shell
.\eba.exe bitcoin augment --ohlcv-filename mapped-block-ohlcv.tsv
```

Note that this process may take a considerable amount of time to complete.
