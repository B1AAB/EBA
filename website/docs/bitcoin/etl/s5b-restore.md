---
title: Restore database dump
description: Step 5b. Load a database dump
sidebar_position: 5
slug: /bitcoin/etl/restore
---




On this page, we walk through the steps to populate an empty Neo4j database using our 
[database dump](/releases/tags/data-releases). 
This approach allows you to skip the resource-intensive 
[bulk import](./import) process 
and start querying the full graph significantly faster.


The process involves:

    *   Downloading the multi-part archive.
    *   Extracting the archive to a local directory.
    *   Loading the dump into your Neo4j instance.

:::tip Do I need to host a graph database?

**Yes,** if you want to 
sample application-specific communities or 
explore the graph interactively 
(e.g., querying $n$-hop neighborhoods).

**No,** if you want 
a quick start for developing models using our 
[generic, pre-sampled communities](https://www.kaggle.com/datasets/aab/bitcoin-graph-sampled-communities).
In this case, you can jump straight to the 
[g101 Jupyter notebook](https://github.com/B1AAB/GraphStudio/blob/main/g101/g101.ipynb) or 
[these quick-start examples](https://github.com/B1AAB/GraphStudio/tree/main/quickstart).
:::


:::danger Resource Requirements 

**Bandwidth:** 
This process involves downloading nearly `1 TB` of data; 
ensure you are using a stable connection without data caps.

**Storage:** Ensure you have at least `4.3 TB` of free disk space 
(compressed download: `~800 GB`, 
extracted database dump: `~800 GB`, and 
populated Neo4j database: `~2.7 TB`).
:::


### Prerequisites & setup

1. Install data source CLI: [install the AWS CLI](https://docs.aws.amazon.com/cli/latest/userguide/getting-started-install.html).
 

2. Install [7-Zip](https://www.7-zip.org).

    ```bash
    sudo apt update && sudo apt install p7zip-full -y
    ```



### Download & extract archive

The database dump is compressed and split into many chunks 
(`1070` chunks, `700 MB` each in [v1](/releases/data-release/v1)) 
to ensure reliable downloading.

1. Configure environment variables to specify the target directories for downloading and extracting the data.

    ```bash
    # Replace path.
    export G_DOWNLOAD_PATH="/mnt/download/path"
    export G_EXTRACT_PATH="/mnt/extract/path"
    ```


2. Sync the dump files.


    ```bash
    aws s3 sync s3://bitcoin-graph/v1/neo4j_db_dump/ "${G_DOWNLOAD_PATH}" --no-sign-request
    ```

3. Extract the multi-part archive. 

    ```bash
    7z x "${G_DOWNLOAD_PATH}/neo4j.dump.gz.001" -o"${G_EXTRACT_PATH}"
    ```

    By targeting the `.001` file, 7-Zip will automatically detect and process the remaining parts in the sequence.

    **Note that** decompressing `~700 GB` of data is a heavy operation, and it will take several hours depending on your disk speed.





### Restore database dump

1. Stop the database service.

    ```shell
    sudo systemctl stop neo4j
    ```

2. Restore the database.

    ```shell
    sudo -u neo4j neo4j-admin database load neo4j \
        --from-path="${G_EXTRACT_PATH}" \
        --overwrite-destination \
        --verbose
    ```

    Please refer to [this page](https://neo4j.com/docs/operations-manual/current/backup-restore/restore-dump/)
    for documentation on the `database load` command.

    **Note:** This step will take a significant amount of time and requires at least `2.722 TiB` of free space in the Neo4j database path.
    

3. Start the database service. 

    ```shell
    sudo systemctl start neo4j
    ```


4. Enable [APOC](https://neo4j.com/docs/apoc/current/installation/).
