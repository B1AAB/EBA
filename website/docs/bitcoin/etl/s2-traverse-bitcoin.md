---
title: Extract Block Data
description: Step 2. Traversing Blocks for Initial Data Extraction
sidebar_position: 1
slug: /bitcoin/etl/traverse
---

In this step, `eba` is used to iterate through a set of blocks and extract relevant data. 
While various data types can be extracted (e.g., tracking UTXOs, identifying newly created addresses), 
the primary focus is on encoding transaction data found within these blocks. 

The following steps describe how to run the `eba traverse` command.


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

Collecting data from the Bitcoin network for all blocks can take a considerable amount of time. 
This code heavily leverages multi-threading, and all the time-consuming operations are implemented non-blocking;
however, there are only so much concurrent requests the Bitcoin client can process optimally. 
EBA is implemented so that it minimizes the latency between submitting API calls and processing the returned data, 
such that the returned data is processed in parallel threads without the application 
waiting for their results to be persisted. However, still, if both EBA and the Bitcoin agent are 
running the same machine, they are bound by the I/O limit of your machine. 
One improvement would be deploying the Bitcoin client on a Kubernetes cluster 
(it will need dockerizing the client) with a load balancer. 
In this setup, you will have multiple instances of the client processing API calls with the 
load balancer directing any new call to appropriate VMs. 
Such a horizontal scale will improve the performance, however, since it requires a k8s cluster setup and VMs on 
the Cloud or on premises HPC, the specifics of this setup are beyond the score of EBA, and are not covered here.



## Optimizing Data Collection Speed

Collecting data from the Bitcoin network, especially across all blocks, can be time-consuming. 
The EBA code leverages multi-threading extensively, and time-consuming operations are implemented 
using non-blocking I/O. However, the Bitcoin client itself can only process a limited number of 
concurrent requests optimally, which can become a bottleneck.

EBA is designed to minimize latency. It processes the data returned from Bitcoin client API calls in parallel threads, 
allowing it to issue new requests without waiting for the results of previous ones to be fully processed and persisted.

Despite these optimizations, if both EBA and the Bitcoin client are running on the same machine, 
performance will still be constrained by the shared hardware resources, particularly I/O limits (disk speed and network bandwidth).

One potential performance improvement involves deploying the Bitcoin client across a Kubernetes (k8s) cluster. 
This requires containerizing the client (e.g., using Docker) and utilizing a load balancer. 
In such a setup, multiple instances of the client handle API calls concurrently, with the load balancer 
distributing requests among them. This horizontal scaling can significantly improve throughput.

However, detailing the specifics of setting up and managing a Kubernetes cluster (whether cloud-based or on-premises) 
is beyond the scope of this EBA documentation and is therefore not covered here.
