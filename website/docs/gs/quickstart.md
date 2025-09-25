---
title: Quick Start
description: Quick Start
sidebar_position: 1
slug: quickstart
---

This quick start provides a hands-on guide to training and evaluating 
a Bitcoin script classification model using pre-sampled communities.

As the following diagram illustrates, 
this guide bypasses the ETL pipeline to focus directly on the machine learning application. 
We skip the ETL pipeline because it requires weeks of processing and 
significant computational resources, which is beyond the scope of a quick start. 
The complete ETL pipeline is [documented separately](/docs/bitcoin/etl/overview).

```mermaid
%%{ 
  init: { 
    'gitGraph': { 
      'mainBranchName': 'Bitcoin-Regular'
    },
    'themeVariables': { 'fontSize': '14px', 'commitLabelFontSize': '14px'}
  } 
}%%


gitGraph:
    commit id: "Sync Node"
    commit id: "Traverse Chain"
    commit id: "Address Stats"
    commit id: "Txo lifecycle"
    commit id: "Import into Neo4j"
    commit id: "Sample Communities"
    branch Bitcoin-Quick-Start
    checkout Bitcoin-Quick-Start
    commit id: "Hello-world Model" 
    checkout Bitcoin-Regular
    merge Bitcoin-Quick-Start
```


## Bitcoin Script Classification

Please follow 
[this documentation](https://github.com/B1AAB/GraphStudio/tree/main/quickstart/script_classification)
for a "hello world" example on using the sampled Bitcoin communities 
for training and evaluating a script classification model.
