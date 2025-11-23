---
title: Import into neo4j
description: Step 5. Import into neo4j
sidebar_position: 4
slug: /bitcoin/etl/import
---


:::tip Do I need to run the bulk import process? 

**Yes,**
if you need to modify the graph structure or append new data that is not included in 
[our release](/releases/tags/data-releases). 
*Note: This is highly resource-intensive and can take 2-3 weeks on a standard desktop.*

**No,** 
if you simply want to explore the graph or sample communities from the dataset. 
In this case, use the [restore database dump](./restore) instead; it bypasses the weeks-long processing time required for the bulk import described on this page. 
:::


[`neo4j-admin`](https://neo4j.com/docs/operations-manual/4.4/tools/neo4j-admin/neo4j-admin-import/)
offers the highest throughput method for populating a massive database. 
Its primary constraint is that it requires an empty database, 
meaning it does not support incremental updates to an existing graph.



<details>
    <summary>Optional: Manual Pre-processing (Experimental)</summary>

    This optional step performs manual deduplication of nodes 
    to improve the Neo4j import process.
    While the Neo4j admin tool offers a `--skip-duplicate-nodes` flag, 
    pre-sorting and deduplicating via the command line is often 
    more memory-efficient for datasets of this scale.


    a.1. Run the _experimental_ application `EXP_PrepareDataForNeo4j`.

        Due to memory constraints, 
        this step aggregates files but does not strictly deduplicate them; 
        it outputs intermediate files intended for sorting.

    a.2. `cd` to the directory where the data is persisted

    a.3. Combine the files:

        ```shell
        cat *_BitcoinTxNode.tsv > combined_BitcoinTxNode.tsv
        ```

        ```shell
        cat *_BitcoinScriptNode.tsv > combined_BitcoinScriptNode.tsv
        ```

    a.4. Sort the files:
        (The goal of the following is to de-duplicate the Tx and Script node files. neo4j has the argument `--skip-duplicate-nodes[=true|false]` that can be used as an alternative to the following.)

        ```shell
        LC_ALL=C sort --buffer-size=32G --parallel=16 --temporary-directory=. -t$'\t' -k1,1 combined_BitcoinTxNode.tsv > sorted_BitcoinTxNode.tsv
        ```

        ```shell
        LC_ALL=C sort --buffer-size=32G --parallel=16 --temporary-directory=. -t$'\t' -k1,1 combined_BitcoinScriptNode.tsv > sorted_BitcoinScriptNode.tsv
        ```

    a.5. Run the _experimental_ application `EXP_ProcessSortedNodeFiles`.
</details>





1.  [Create a neo4j database](https://neo4j.com/docs/desktop/current/operations/database-management/#_create_a_new_database).
    
2.  If you are using an existing database instance,
    ensure the target database is empty and shut down.

    ```shell
    sudo systemctl stop neo4j
    ```

3.  Set an environment variable pointing to the directory containing the Bitcoin graph TSV files.


    ```shell
    export GDIR="<set to the dir that contains graph data>"
    ```

    Verify that your data directory contains the correctly batched files. 
    You can use the following command to inspect the file distribution by type 
    (ignoring timestamp prefixes):


    ```shell
    ls -1 | sed -E "s/^[0-9]+_/[Timestamp]_/" | sort | uniq -c
    ```

    You should see header files (e.g., `BitcoinGraph_header.tsv.gz`), 
    batched edge files (e.g., `195 [Timestamp]_BitcoinS2S.tsv.gz`), and the unique node files.

    ```text 
    1   BitcoinB2S_header.tsv.gz
    1   BitcoinB2T_header.tsv.gz
    1   BitcoinC2S_header.tsv.gz
    1   BitcoinC2T_header.tsv.gz
    1   BitcoinCoinbase.tsv.gz
    1   BitcoinGraph_header.tsv.gz
    1   BitcoinS2B_header.tsv.gz
    1   BitcoinS2S_header.tsv.gz
    1   BitcoinScriptNode_header.tsv.gz
    1   BitcoinT2B_header.tsv.gz
    1   BitcoinT2T_header.tsv.gz
    1   BitcoinTxNode_header.tsv.gz
    195 [Timestamp]_BitcoinC2S.tsv.gz
    195 [Timestamp]_BitcoinC2T.tsv.gz
    1   [Timestamp]_BitcoinGraph.tsv.gz
    195 [Timestamp]_BitcoinS2S.tsv.gz
    195 [Timestamp]_BitcoinT2T.tsv.gz
    195 [Timestamp]_byC2S_BitcoinB2S.tsv.gz
    195 [Timestamp]_byC2T_BitcoinB2T.tsv.gz
    195 [Timestamp]_byS2S_BitcoinB2S.tsv.gz
    195 [Timestamp]_byS2S_BitcoinS2B.tsv.gz
    195 [Timestamp]_byT2T_BitcoinB2T.tsv.gz
    195 [Timestamp]_byT2T_BitcoinT2B.tsv.gz
    1   unique_BitcoinScriptNode.tsv.gz
    1   unique_BitcoinTxNode.tsv.gz
    ```

4.  Determine the optimal heap size for the import process. 
    For a graph of this magnitude, memory configuration is critical for performance. 
    Please refer to [Neo4j Memory Configuration Guide]((https://neo4j.com/docs/operations-manual/current/performance/memory-configuration/)).


5.  Execute the import command. 
    Note that we use regex patterns (e.g., .`*BitcoinS2S.tsv.gz`) to ingest the batched edge files automatically.

    ```shell
    sudo -u neo4j HEAP_SIZE=4G neo4j-admin database import full \
        --overwrite-destination neo4j \
        --nodes="$GDIR/BitcoinCoinbase.tsv.gz" \
        --nodes="$GDIR/BitcoinGraph_header.tsv.gz,$GDIR/0_BitcoinGraph.tsv.gz" \
        --nodes="$GDIR/BitcoinScriptNode_header.tsv.gz,$GDIR/unique_BitcoinScriptNode.tsv.gz" \
        --nodes="$GDIR/BitcoinTxNode_header.tsv.gz,$GDIR/unique_BitcoinTxNode.tsv.gz" \
        --relationships="$GDIR/BitcoinS2S_header.tsv.gz,$GDIR/.*BitcoinS2S.tsv.gz" \
        --relationships="$GDIR/BitcoinT2T_header.tsv.gz,$GDIR/.*BitcoinT2T.tsv.gz" \
        --relationships="$GDIR/BitcoinC2T_header.tsv.gz,$GDIR/.*BitcoinC2T.tsv.gz" \
        --relationships="$GDIR/BitcoinC2S_header.tsv.gz,$GDIR/.*BitcoinC2S.tsv.gz" \
        --relationships="$GDIR/BitcoinB2T_header.tsv.gz,$GDIR/.*BitcoinB2T.tsv.gz" \
        --relationships="$GDIR/BitcoinB2S_header.tsv.gz,$GDIR/.*BitcoinB2S.tsv.gz" \
        --relationships="$GDIR/BitcoinS2B_header.tsv.gz,$GDIR/.*BitcoinS2B.tsv.gz" \
        --relationships="$GDIR/BitcoinT2B_header.tsv.gz,$GDIR/.*BitcoinT2B.tsv.gz" \
        --delimiter="\t" \
        --array-delimiter=";" \
        --verbose \
        --skip-duplicate-nodes
    ```


6.  Once the import concludes, restart the Neo4j service. 
    We also recommend installing the 
    [APOC library](https://neo4j.com/docs/apoc/current/installation/), 
    as it is needed in EBA for sampling communities.
