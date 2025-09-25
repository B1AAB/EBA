EBA interfaces with the Bitcoin network and 
creates a graph of the full history of transactions recorded on-chain, 
encompassing the complete trading details of over `8.72` billion BTC.
The temporal heterogeneous graph consists of over `2.4` billion nodes and 
`39.72` billion time-stamped edges spanning more than a decade, 
making it a complete resource for developing models on Bitcoin and 
a large-scale resource for benchmarking graph neural networks. 


We share the complete ETL pipeline and all the data it generates. 
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

    %% --- Styling ---
    %%style eba fill:#ff9e00,stroke:#ff9e00,color:#000
    style eba fill:#5a189a,stroke:#5a189a,color:#fff
```
