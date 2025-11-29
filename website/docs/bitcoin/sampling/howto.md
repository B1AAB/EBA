---
id: howto
title: Sample Your Own Communities
sidebar_label: Sample Communities
---


-   [Setup a Neo4j database](/docs/gs/graphdb)

-   [Restore](/docs/bitcoin/etl/restore) the graph database if needed.

-   Ensure the database is running and you're connected to it. 

-   Run the sampling method; you may run the following command for a documentation on the command's arguments.

    ```bash
    .\eba.exe bitcoin sample --help
    ```

    Alternatively, you can provide the command's arguments using a JSON file. You may use the following as an example.

    ```json
    {
        "GraphSample": {
            "Count": 10,
            "Hops": 3,
            "MinNodeCount": 500,
            "MaxNodeCount": 1000,
            "MinEdgeCount": 499,
            "MaxEdgeCount": 10000,
            "MaxAttempts": 25,
            "MaxNodeFetchFromNeighbor": 10000,
            "MaxEdgesFetchFromNeighbor": 500000,
            "SerializeEdges": false,
            "SerializeFeatureVectors": true,
            "ForestFireNodeSamplingCountAtRoot": 100,
            "ForestFireMaxHops": 2,
            "ForestFireQueryLimit": 1000,
            "ForestFireNodeCountReductionFactorByHop": 4,
            "RootNodeSelectProb": 0.3
        }
    }
    ```

    You may then run the tool using the JSON file as the following. 

    ```bash
    .\eba.exe bitcoin sample --status-filename .\my_options.json
    ```
