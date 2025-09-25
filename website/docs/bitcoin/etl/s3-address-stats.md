---
title: Address Statistics
description: Step 3. Update stats
sidebar_position: 2
slug: /bitcoin/etl/address-stats
---

_This is an optional section, and it details a process that requires address tracking to have been enabled during the initial blockchain traversal command._
_You may skip this step if you do not intent to get per-block statistics (summary statistics about bitcoin network in general)._

The goal is to calculate various block-level address statistics, 
such as identifying new vs. previously seen addresses, 
and counting unique and total addresses per block. 
Generating these stats involves efficient uniqueness 
checking across potentially vast amounts of data. 
While in-memory checks work for small ranges, 
they don't scale to the full blockchain range. 
We also found that using a database for real-time tracking 
introduced significant overhead that slowed the main traversal, 
even when performed asynchronously.


To ensure scalability without impacting traversal performance, 
we utilize a two-step post-processing approach:

* Address Logging (During Traversal): 
    All observed addresses are simply appended to plain text files 
    as they are encountered during the block traversal. 
    No costly indexing or uniqueness checks are performed at this stage.

* Offline Analysis (Post-Traversal):
    First, the large address log files are sorted using scalable external 
    sorting methods (which efficiently utilize both disk and memory).
    Then, a custom tool processes the resulting sorted list
    and the calculation of all necessary statistics.


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

