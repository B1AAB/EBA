---
title: Installation
description: Install EBA
sidebar_position: 1
slug: ./installation
---

import Tabs from '@theme/Tabs';
import TabItem from '@theme/TabItem';



:::tip Do I need to install EBA?
**Yes**, if you want to 
[reproduce or update the data](/docs/bitcoin/etl/overview), or 
[sample custom communities](/docs/bitcoin/sampling/overview) 
from the graph.

**No**, if you only want to use the 
[communities](https://www.kaggle.com/datasets/vjalili/bitcoin-graph-sampled-communities), or 
the [sample models](https://github.com/B1AAB/GraphStudio) we provide.
::::

## Build from source code

You may take the following steps to build EBA from the source code. 

1. Clone the git repository. 

    <Tabs
        groupId="operating-systems"
        defaultValue="linux"
        values={[
            { label: 'Linux', value: 'linux' },
            { label: 'macOS', value: 'mac' },
            { label: 'Windows', value: 'windows' }
        ]
    }>
    <TabItem value="linux">

    ```bash
    git clone https://github.com/b1aab/eba && cd eba
    ```

    </TabItem>
    <TabItem value="mac">

    ```bash
    git clone https://github.com/b1aab/eba && cd eba
    ```

    </TabItem>
    <TabItem value="windows">

    ```bash
    git clone https://github.com/b1aab/eba ; cd eba
    ```

    </TabItem>
    </Tabs>

2. Build EBA.

    <Tabs
        groupId="operating-systems"
        defaultValue="linux"
        values={[
            { label: 'Linux', value: 'linux' },
            { label: 'macOS', value: 'mac' },
            { label: 'Windows', value: 'windows' }
        ]
    }>
    <TabItem value="linux">

    ```bash
    dotnet publish ./EBA/EBA.csproj -c Release -r linux-x64 --self-contained true -o "build" -p:WarningLevel=0
    ```

    </TabItem>
    <TabItem value="mac">

    ```bash
    dotnet publish ./EBA/EBA.csproj -c Release -r osx-arm64 --self-contained true -o "build" -p:WarningLevel=0
    ```

    </TabItem>
    <TabItem value="windows">

    ```bash
    dotnet publish .\EBA\EBA.csproj -c Release -r win-x64 --self-contained true -o "build" -p:WarningLevel=0
    ```

    </TabItem>
    </Tabs>


3. Use EBA.

    <Tabs
        groupId="operating-systems"
        defaultValue="linux"
        values={[
            { label: 'Linux', value: 'linux' },
            { label: 'macOS', value: 'mac' },
            { label: 'Windows', value: 'windows' }
        ]
    }>
    <TabItem value="linux">

    ```bash
    cd build/ && ./eba --help
    ```

    </TabItem>
    <TabItem value="mac">

    ```bash
    cd build/ && ./eba --help
    ```

    </TabItem>
    <TabItem value="windows">

    ```bash
    cd .\build\ ; .\eba.exe --help
    ```

    </TabItem>
    </Tabs>



## Install Neo4j {#neo4j}

You can run the Neo4j Graph Database in several ways, 
such as a self-hosted production cluster or a fully managed, 
[cloud-based solution](https://neo4j.com/deployment-center).


For development and accessibility, 
all our solutions are designed to run on a standalone Neo4j Desktop installation, 
although a cloud-based deployment can be more performant.

Therefore, please follow [this documentation](https://neo4j.com/docs/operations-manual/current/installation) 
for installing the Neo4j Desktop version.

