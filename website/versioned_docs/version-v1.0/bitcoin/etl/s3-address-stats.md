---
title: Address Statistics
description: Step 3. Update stats
sidebar_position: 2
slug: /bitcoin/etl/address-stats
---

:::info
This is an optional step, 
and it details a process that requires address tracking to have been enabled during `eba bitcoin traverse`.

You may skip this step if you do not intent to get per-block statistics.
:::


The goal is to calculate various block-level address statistics, 
such as identifying new vs. previously seen addresses, 
and counting unique and total addresses per block. 


Generating these statistics requires efficient uniqueness checking 
across vast amounts of data. 
While in-memory checks are effective for small data, 
they don't scale to a large list of Bitcoin addresses. 
We also found that using a database (e.g., PostgreSQL) 
to track address uniqueness during the traversal process 
introduced significant overhead.


Therefore, we utilize a two-step post-processing approach:

* Step 1: Address Logging During Traversal: 
    During the `traverse` command, all addresses are simply 
    appended to plain text files. 
    No costly indexing or uniqueness checks are performed at this stage.

* Step 2: Disk-based Post-Traversal Uniqueness Check:
    First, the large addresses text file from step 1 are sorted using a disk-based sorting method.
    Then, a custom tool processes the sorted list and the calculation of all necessary statistics.


The specific steps are outlined below .


1. Merge all the files (you may skip this if you have only one file): 

    ```shell
    cat *_addresses.tsv > combined_addresses.tsv
    ```

2. Sorted the merged file: 

    ```shell
    LC_ALL=C sort --buffer-size=32G --parallel=16 --temporary-directory=. -t$'\t' -k1,1 combined_addresses.tsv -o sorted_combined_addresses.tsv

    real    1166m35.442s
    user    62m13.415s
    sys     33m40.558s
    ```

    This uses 32G RAM and uses disk for temporarily storing files needed for sorting large files. 
    You may adjust the RAM size and make sure enough storage space on the specified paths. 


3. Run the `EXP_AddAddressStatsToBlockStats` project. (Experimental)

