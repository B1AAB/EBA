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
*Note: This is highly resource-intensive and can take 2-3 weeks on a high-end desktop computer.*

**No,** 
if you simply want to explore the graph or 
[sample communities](/docs/bitcoin/sampling/overview) from the dataset. 
In this case, [restore database dump](./restore) instead; it bypasses the weeks-long processing time required for the bulk import described on this page. 
:::


On this page, we walk through the steps required to import the Bitcoin graph from batched TSV files into a Neo4j database.


### Checkpoint: using pre-generated graph data

If you chose to skip the 
[sync a Bitcoin node](./node-sync) $\rightarrow$ 
[traverse](./traverse) steps, you do _not_ have the TSV files yet. 
Instead, you can download the data we have prepared, 
which encompasses all blocks up to height `863000`.

_Note: If you **did run** the 
[sync a Bitcoin node](./node-sync) and 
[traverse](./traverse) steps and 
generated your own TSV files, you can skip this step and 
proceed directly to the [import](#import) step below._

:::danger Resource Requirements 

This process involves downloading nearly `1.2 TB` of data; 
ensure you are using a stable connection without data caps and 
have at least `1.2 TB` of free disk space. 
:::

You may take the following steps to download the graph in TSV files.

1. [Install the AWS CLI](https://docs.aws.amazon.com/cli/latest/userguide/getting-started-install.html).

2. Configure environment variable to specify the target directory.

    ```shell
    export GDIR="/mnt/download/path"
    ```

3. Download the TSV files. 

    ```shell
    aws s3 sync s3://bitcoin-graph/v1/data_to_import_neo4j/ "${GDIR}" --no-sign-request    
    ```



### Import into the database {#import}

[`neo4j-admin`](https://neo4j.com/docs/operations-manual/4.4/tools/neo4j-admin/neo4j-admin-import/)
offers the highest throughput method for populating a massive database. 
Its primary constraint is that it requires an empty database, 
meaning it does not support incremental updates to an existing graph.


1.  Install [neo4j graph database](/docs/gs/graphdb#neo4j).

2.  [Create a neo4j database](https://neo4j.com/docs/desktop/current/operations/database-management/#_create_a_new_database).
    
3.  If you are using an existing database instance,
    ensure the target database is empty and shut down.

    ```shell
    sudo systemctl stop neo4j
    ```

4.  Set an environment variable pointing to the directory containing the Bitcoin graph TSV files.


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

5.  Determine the optimal heap size for the import process. 
    For a graph of this magnitude, memory configuration is critical for performance. 
    Please refer to [Neo4j Memory Configuration Guide](https://neo4j.com/docs/operations-manual/current/performance/memory-configuration/).


6.  Execute the import command. 
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


7.  Once the import concludes, restart the Neo4j service. 
    We also recommend installing the 
    [APOC library](https://neo4j.com/docs/apoc/current/installation/), 
    as it is needed in EBA for sampling communities.
