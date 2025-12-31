---
title: Extract Block Data
description: Step 2. Traversing Blocks for Initial Data Extraction
sidebar_position: 1
slug: /bitcoin/etl/traverse
---

EBA connects to a [fully synchronized Bitcoin Core](./s1-sync-bitcoin.mdx) 
node and iterates through a set of blocks, 
extracts transaction data, and encodes them as temporal heterogeneous graph. 

For this task, you may take the following steps.

- [Install the program](/gs/installation.md), if you have not installed already.

- Make sure `Bitcoin Core` is running and responding to API calls (see [this page](./s1-sync-bitcoin.mdx)).

- Run `eba`.

    ```shell
    .\eba.exe bitcoin traverse --from 0 --to 1000
    ```

    or if you want to track txo (for downstream statistics only) and the traverse window is wide, then you may use:

    ```shell
    .\eba.exe bitcoin traverse --to 863000 --track-txo --max-entries-per-batch 50000000
    ```

    You may use the following to get all the arguments and their documentation.

    ```shell
    .\eba.exe bitcoin traverse --help
    ```


### Performance and Scalability

Traversing Bitcoin blocks can take a considerable amount of time. 
To accelerate this, 
EBA heavily leverages multi-threading, 
and all time-consuming operations are implemented to be non-blocking. 
It also minimizes the latency between submitting API calls and 
processing the returned data, 
which allows data to be handled in parallel threads, 
so it doesn't wait to encode and persist a block's graph elements before processing the next block.
However, there is a limit to how many concurrent requests EBA and Bitcoin Core can process optimally. 
Therefore, despite these optimizations, 
if both applications are running on the same machine, 
their performance is ultimately bound by its I/O limits, 
primarily the random read/write performance of the storage.


Since EBA processes each block independently, 
one potential improvement is to deploy the application on a Kubernetes (k8s) cluster 
(requires dockerizing both EBA and Bitcoin Core).
In this setup, each instance of EBA service could process a subset of blocks 
while a load balancer directs its API calls to replicas of the Bitcoin Core services. 
This horizontal scaling would significantly improve performance; 
however, because this requires a k8s cluster and cloud or on-premises HPC resources 
that may not be [widely accessible](/docs/gs/accessibility), 
the specifics of such a deployment are not currently covered.



## Deduplicate Nodes

This step deduplicates the 
`Tx` (`[0-9]*_BitcoinTxNode.csv.gz`) and 
`Script` (`[0-9]*_BitcoinScriptNode.csv.gz`) 
node files.

<details>

    <summary>Why is deduplication required?</summary>

    For performance reasons, 
    EBA does not attempt to ensure uniqueness in the `Tx` and `Script` files 
    during the initial traversal. 
    Instead, it writes nodes as it encounters them. 
    A `Tx` node is created once when the block containing the transaction is parsed, 
    and again every time that transaction is referenced as a `txin` in subsequent blocks. 
    The same applies to `Script` nodes. 
    Consequently, the `Tx` and `Script` nodes files contain a high degree of duplication.


    Crucially, the instance of the node created where the transaction 
    originated contains detailed information, 
    whereas references in subsequent blocks contain minimal information. 
    This means some duplicate entries are rich in data while others 
    are missing feature values. 
    Therefore, we must deduplicate the files by "merging" duplicates 
    into a single node that retains values for all features.


    This step is critical for the Neo4j import process. 
    While the Neo4j admin tool offers a `--skip-duplicate-nodes` flag, 
    relying on it is discouraged because it only tolerates 
    a limited number of duplicates, 
    and increasing that limit incurs significant performance penalties. 
    Furthermore, Neo4j does not support the merging of data described above; 
    it would simply discard subsequent entries.

</details>


1.  `cd` to the directory where the data is persisted.

2.  Combine the files:

    ```shell
    zcat [0-9]*_BitcoinTxNode.csv.gz > combined_BitcoinTxNode.csv
    ```

    ```shell
    zcat [0-9]*_BitcoinScriptNode.csv.gz > combined_BitcoinScriptNode.csv
    ```

3.  Sort the files. 
    Note: Since these files can be very large, 
    the command below is configured to use temporary on-disk files. 
    Ensure you have sufficient disk space (at least as much as the `combined_*` files) 
    and are running on performant media (e.g., NVMe). 
    Adjust the `--buffer-size` according to the available memory on your machine.


    ```shell
    LC_ALL=C sort --buffer-size=32G --parallel=16 --temporary-directory=. -t$'\t' -k1,1 combined_BitcoinTxNode.csv > sorted_BitcoinTxNode.csv
    ```

    ```shell
    LC_ALL=C sort --buffer-size=32G --parallel=16 --temporary-directory=. -t$'\t' -k1,1 combined_BitcoinScriptNode.csv > sorted_BitcoinScriptNode.csv
    ```

4.  Run the following command to deduplicate the files:

    ```shell
    .\eba.exe bitcoin dedup --sorted-script-nodes-file sorted_BitcoinScriptNode.csv --sorted-tx-nodes-file sorted_BitcoinTxNode.csv
    ```
