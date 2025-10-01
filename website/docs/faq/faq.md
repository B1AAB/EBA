---
title: FAQ
description: FAQ
sidebar_position: 0
---

* `httpclientpolicy ... service unavailable`

    This warning commonly occurs when traversing the bitcoin network without using the helper txo database.
    If you get this warning, you may ignore it as EBA will most likely automatically recover from it. 
    The warning means EBA is making more concurrent RPC calls to Bitcoin-qt that exceeds its threshold. 
    You can change the threshold by increasing the value of `-rpcworkqueue` when you start bitcoin-qt. 
    When this limit is reached, EBA will safely wait and retry a few times after dynamically determined 
    wait intervals (so to lower the load on bitcoin-qt). However, if EBA still fails to fetch data 
    for the fail request, it will fail that block, store the failed block in the faild blocks list, 
    will safely continue with the rest of the blocks. 


* Can the output of a transaction be referenced as an input for another transaction in the **same block**?

    Yes, the output of a transaction can be referenced as an input for another transaction in the same block. For instance, in the block with hash 

    ```
    0000000000000000000cfa4e0939572c39cdaa8d58a275ae22e5877fc925b91a
    ```

    the transaction with ID

    ```
    a68bb8474920375010f7941f5f0b7261194365fbfa916fb1cdc6d726accb9a81
    ```

    is created and its output is referenced as an input in the same block.
