**EBA addresses a long-standing issue that has hindered 
the ML community from developing applications for Bitcoin: 
a lack of ML-first data.** 
EBA interfaces with the Bitcoin network and 
creates a graph of the full history of transactions recorded on-chain. 
On this graph, the nodes are Bitcoin _scripts_ (aka _addresses_), 
and the edges between them represent transactions recorded on-chain via the UTxO model.
Simply put, 
the graph represents a transaction between _addresses_ `x` and `y` as 
a time-stamped, directed edge between their corresponding nodes. 
Consequently, the flow of how funds are earned and spent can be traced 
by traversing these paths.

<p align="center">
    |
    <a href="https://eba.b1aab.ai/docs/gs/welcome">Read More</a> 
    |
</p>



This graph is built for machine learning, 
particularly Graph Neural Networks (GNNs),
allowing a graph-based model to 
aggregate information from neighbors through message passing and 
learn the topology of fund flows for various applications 
across the vibrant cryptocurrency ecosystem, including:

*   Exploring economic evolution and temporal behaviors.
*   Analyzing network dynamics and trading patterns.
*   Identifying suspicious or illicit activities.
*   Benchmarking large-scale, graph-based machine learning models.

<p align="center">
    |
    <a href="https://github.com/B1AAB/GraphStudio">Explore Example Applications</a> 
    |
</p>


The Bitcoin Graph that EBA creates encompasses 
the complete trading details of over `8.72` billion BTC,
and it consists of over `2.4` billion nodes and 
`39.72` billion time-stamped edges spanning more than a decade.
We share the complete ETL pipeline and all the data it generates.

<p align="center">
    |
    <a href="https://eba.b1aab.ai">Documentation</a> 
    |
    <a href="https://eba.b1aab.ai/docs/gs/quickstart">Quick Start</a> 
    |
    <a href="https://eba.b1aab.ai/releases/tags/data-releases">Data Releases</a> 
    |
</p>



To simplify working with the pipeline and its resources, 
we have split them into separate repositories. 
The following resource map will help you navigate to the resources that suit your application. 


```mermaid
graph LR
    bitcoin{{Bitcoin Core}} --> eba(EBA);
    eba --> tsv[\Graph in TSV\];
    eba --> neo4j[\Graph in Neo4j\];
    neo4j --> neo4jDump[\Neo4j dump\];

    subgraph gStudio[Graph Studio]
        direction TB

        apps>Applications];

        offchain[/Off-chain Resources/];

        %%offchain --> apps;
    end

    eba --> coms[\Sampled Communities\];
    coms --> apps;    

    %% --- Link Definitions ---
    click eba "https://github.com/B1AAB/EBA" "EBA"
    click offchain "https://github.com/B1AAB/GraphStudio/blob/main/off_chain_resources/" "Off-chain resources"
    click apps "https://github.com/B1AAB/GraphStudio/blob/main/quickstart/" "Off-chain resources"
    click neo4j "https://drive.google.com/drive/folders/11X6QiVvWSOzxvDIAD0OWu3g2Sa0as3UQ?usp=sharing" "Graph in Neo4j"
    click neo4jDump "https://drive.google.com/drive/folders/1bAsjgVaIQrG2TDGkMtIEDPX8xKoiHJUf?usp=sharing" "Neo4j Dump"
    click coms "https://www.kaggle.com/datasets/aab/bitcoin-graph-sampled-communities" "Sampled Communities"
    click tsv "https://eba.b1aab.ai/releases/data-release/v1" "Graph in TSV"

    %% --- Styling ---
    %%style eba fill:#ff9e00,stroke:#ff9e00,color:#000
    style eba fill:#5a189a,stroke:#5a189a,color:#fff
```