---
title: Import into neo4j
description: Step 5. Import into neo4j
sidebar_position: 4
slug: /bitcoin/etl/import
---


Neo4j's [neo4j-admin import tool](https://neo4j.com/docs/operations-manual/4.4/tools/neo4j-admin/neo4j-admin-import/) 
offers the fastest method for initially populating a database. 
Its main drawback is that it requires the database to be empty;
hence, it doesn't support incremental updates. 
Therefore, we provide separate solutions optimized for both 
initial population and incremental updates.


## Initial Data Load {#full}

1.  [Experimental] you need to run a script that takes batches CSV files, 
    combines them into single file per node or relationship type, and formats them 
    in a way that neo4j admin tool can use it. 

    1.1. Run:

    ```shell
    EXP_PrepareDataForNeo4j
    ```

    Since we need to avoid duplicates in node definitions, and due to file size and memory usage contraints, 
    this first step will not attempt to avoid duplicates, instead it will output files
    whose duplicates will be removed in the following steps.

    1.2. `cd` to the directory where the data is persisted

    1.3. Combine the files:

    ```shell
    cat *_BitcoinTxNode.tsv > combined_BitcoinTxNode.tsv
    ```

    ```shell
    cat *_BitcoinScriptNode.tsv > combined_BitcoinScriptNode.tsv
    ```

    1.4. Sort the files:
    (The goal of the following is to de-duplicate the Tx and Script node files. neo4j has the argument `--skip-duplicate-nodes[=true|false]` that can be used as an alternative to the following.)

    ```shell
    LC_ALL=C sort --buffer-size=32G --parallel=16 --temporary-directory=. -t$'\t' -k1,1 combined_BitcoinTxNode.tsv > sorted_BitcoinTxNode.tsv
    ```

    ```shell
    LC_ALL=C sort --buffer-size=32G --parallel=16 --temporary-directory=. -t$'\t' -k1,1 combined_BitcoinScriptNode.tsv > sorted_BitcoinScriptNode.tsv
    ```

    1.5. Run the application `EXP_ProcessSortedNodeFiles`


2. [Create a neo4j database](https://neo4j.com/docs/desktop/current/operations/database-management/#_create_a_new_database), 
    or if using an existing database, make sure it is empty.

3. Stop the database if it is running.

4. `cd` to the database's import directory, for instance: 

    ```
    C:\neo4j\relate-data\dbmss\dbms-5739a8c7-7235-4e8a-a2ad-7b708514efce
    ```

5. Use the following command to load data into neo4j empty data base using the admin tools.

    ```powershell
    $ENV:GDIR=""  # set to the directory containing graph data without the trailing `\`
    ```

    ```powershell
    $ENV:HEAP_SIZE = "96G"  # set the heap size for the neo4j-admin tool
    ```

    Note that if you change the name of the database in the following, you will need to 
    create that database in neo4j first, then run the import command.

    ```powershell title="neo4j admin"
    .\bin\neo4j-admin.ps1 database import full --overwrite-destination neo4j `
    --nodes="$ENV:GDIR\BitcoinCoinbase.tsv.gz" `
    --nodes="$ENV:GDIR\BitcoinGraph_header.tsv.gz,$ENV:GDIR\0_BitcoinGraph.tsv.gz" `
    --nodes="$ENV:GDIR\BitcoinScriptNode_header.tsv.gz,$ENV:GDIR\unique_BitcoinScriptNode.tsv.gz" `
    --nodes="$ENV:GDIR\BitcoinTxNode_header.tsv.gz,$ENV:GDIR\unique_BitcoinTxNode.tsv.gz" `
    --relationships="$ENV:GDIR\BitcoinS2S_header.tsv.gz,$ENV:GDIR\.*BitcoinS2S.tsv.gz" `
    --relationships="$ENV:GDIR\BitcoinT2T_header.tsv.gz,$ENV:GDIR\.*BitcoinT2T.tsv.gz" `
    --relationships="$ENV:GDIR\BitcoinC2T_header.tsv.gz,$ENV:GDIR\.*BitcoinC2T.tsv.gz" `
    --relationships="$ENV:GDIR\BitcoinC2S_header.tsv.gz,$ENV:GDIR\.*BitcoinC2S.tsv.gz" `
    --relationships="$ENV:GDIR\BitcoinB2T_header.tsv.gz,$ENV:GDIR\.*BitcoinB2T.tsv.gz" `
    --relationships="$ENV:GDIR\BitcoinB2S_header.tsv.gz,$ENV:GDIR\.*BitcoinB2S.tsv.gz" `
    --relationships="$ENV:GDIR\BitcoinS2B_header.tsv.gz,$ENV:GDIR\.*BitcoinS2B.tsv.gz" `
    --relationships="$ENV:GDIR\BitcoinT2B_header.tsv.gz,$ENV:GDIR\.*BitcoinT2B.tsv.gz" `
    --delimiter "\t" --array-delimiter ";" --verbose
    ```

    ```powershell title="neo4j admin (skip duplicates)"
    .\bin\neo4j-admin.ps1 database import full --overwrite-destination neo4j `
    --nodes="$ENV:GDIR\BitcoinCoinbase.tsv.gz" `
    --nodes="$ENV:GDIR\BitcoinGraph_header.tsv.gz,$ENV:GDIR\0_BitcoinGraph.tsv.gz" `
    --nodes="$ENV:GDIR\BitcoinScriptNode_header.tsv.gz,$ENV:GDIR\unique_BitcoinScriptNode.tsv.gz" `
    --nodes="$ENV:GDIR\BitcoinTxNode_header.tsv.gz,$ENV:GDIR\unique_BitcoinTxNode.tsv.gz" `
    --relationships="$ENV:GDIR\BitcoinS2S_header.tsv.gz,$ENV:GDIR\.*BitcoinS2S.tsv.gz" `
    --relationships="$ENV:GDIR\BitcoinT2T_header.tsv.gz,$ENV:GDIR\.*BitcoinT2T.tsv.gz" `
    --relationships="$ENV:GDIR\BitcoinC2T_header.tsv.gz,$ENV:GDIR\.*BitcoinC2T.tsv.gz" `
    --relationships="$ENV:GDIR\BitcoinC2S_header.tsv.gz,$ENV:GDIR\.*BitcoinC2S.tsv.gz" `
    --relationships="$ENV:GDIR\BitcoinB2T_header.tsv.gz,$ENV:GDIR\.*BitcoinB2T.tsv.gz" `
    --relationships="$ENV:GDIR\BitcoinB2S_header.tsv.gz,$ENV:GDIR\.*BitcoinB2S.tsv.gz" `
    --relationships="$ENV:GDIR\BitcoinS2B_header.tsv.gz,$ENV:GDIR\.*BitcoinS2B.tsv.gz" `
    --relationships="$ENV:GDIR\BitcoinT2B_header.tsv.gz,$ENV:GDIR\.*BitcoinT2B.tsv.gz" `
    --delimiter "\t" --array-delimiter ";" --verbose --skip-duplicate-nodes

6. Enable [APOC](https://neo4j.com/docs/apoc/current/installation/).


## Incremental Update {#incremental}

1. Enable [APOC](https://neo4j.com/docs/apoc/current/installation/).

2. Make sure to increase the max heap size for neo4j, otherwise you may get the following error message:

    > There is not enough memory to perform the current task.
    > Please try increasing 'dbms.memory.heap.max_size' in
    > the neo4j configuration (normally in 'conf/neo4j.conf'
    > or, if you are using Neo4j Desktop, found through the
    > user interface) or if you are running an embedded
    > installation increase the heap by using '-Xmx'
    > command line flag, and then restart the database.

3. Run (Use `--help` for usage docs):

    ```
    eba bitcoin import
    ```




## Load database dump {#load}

We also provide a dump of the neo4j database containing the entire bitcoin graph.
In order to use this, you may take the following steps:

1. Download the dump from the following link:

    ```shell
    # LINK
    ```

2. Import into neo4j. You may follow the steps outlined 
[on this page](https://neo4j.com/docs/operations-manual/current/backup-restore/restore-dump/)
for importing the downloaded database dump. Or, you may run the following.


    ```
    .\bin\neo4j-admin.bat database load --overwrite-destination=true --verbose --from-path=M:\\ neo4j
    ```

    This process may take a few hours and needs 2.722TiB storage. 
    If it asks for password, you may enter `password`.

3. Enable [APOC](https://neo4j.com/docs/apoc/current/installation/).
