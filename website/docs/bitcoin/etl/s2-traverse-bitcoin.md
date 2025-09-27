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

- Make sure `bitcoin-qt` is running and responding to API calls (see [this page](./s1-sync-bitcoin.mdx)).

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
