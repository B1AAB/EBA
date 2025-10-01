---
title: TXO Lifecycle
description: Step 4. Tracking TXO Spending History
sidebar_position: 3
slug: /bitcoin/etl/txo-lifecycle
---


:::info
This is an optional step, 
and you may run this step only if you included the `--track-txo` flag [when running the `traverse` command.](/docs/bitcoin/etl/traverse)

You may skip this step if you do not intent to get TXO lifecycle statistics.
:::


1. Merge all the files (you may skip this if you have only one file): 

    ```shell
    cat *_bitcoin_txo.tsv > combined_bitcoin_txo.tsv
    ```

2. Sort the file

    ```shell
    sort --buffer-size=32G --parallel=16 --temporary-directory=. -t$'\t' -k1,1 combined_bitcoin_txo.tsv -o sorted_combined_bitcoin_txo.tsv
    ```

3. Run the `TEMP_ProcessTxoFileSorted` project. (Experimental)
