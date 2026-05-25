**This file contains references for extending testing to more comprehensive cases.**

## Fee
Transaction fee is paid by the requester; for instance, see:

```json
"txid": "882004308069eab12cfb24708b8121d72b97d755c916aa8900bb99210d7796a7"
```

where input value is `792.8964` and `637.7955` is change 
(returned to the requesters address), `155.1` is paid to 
the destination address, and fee is `0.0009`. In this example:

```json
792.8964 = 637.7955 + 0.0009 + 155.1
```

where after applying the change: 

```json
155.1009 = 0.0009 + 155.1
```


## Address to payment type association

https://bitcoin.stackexchange.com/a/75124/129532


Will all transactions have address associated with them? No!

https://bitcoin.stackexchange.com/a/96866/129532


## Tx

See the following pages for different aspects of transactions for better testing.

- https://developer.bitcoin.org/devguide/transactions.html
- https://developer.bitcoin.org/reference/transactions.html
- https://www.lopp.net/pdf/Bitcoin_Developer_Reference.pdf


## Coinbase Tx

Bitcoins in a coinbase transaction cannot 
be spent until they've received 100 confirmations in the blockchain,
in other words, these coins cannot spent for roughly 16h and 40min. 

More details on the coinbase tx:

- https://www.geeksforgeeks.org/what-is-coinbase-transaction/
- https://academy.bit2me.com/en/what-is-coinbase-transaction/


## Locking scripts
Bitcoin is a programmable currency, and users can lock their 
assets in a number of ways. The Bitcoin community has 
accepted some "standard" locking scripts: 

https://bitcoin.stackexchange.com/a/91090/129532


## Some blocks to test corner cases
 
- `71036`
- `132317`
  - `tx: 7882735f55dfdb9251e6e6b9124e0e0dae44564c5daa9a831fd6ac2b9bd16e8e`
  
     This tx has `0` output and all the input goes to the miner as fee.
	 
	 
## Blocks with more than one output address in the coinbase transaction: 

- Height = 79764
This block has two outputs, no fee.

- Other examples where fee is 0.0: height=79802, height=80661, height=80737
- fee >= 0: height=84482 


See [this](https://bitcoin.stackexchange.com/a/105833/129532) post on 
the usecase of multiple outputs in a coninbase transaction.



- a block with a transaction with fee > 0 (maybe the first block):
2817




## Unit tests to implement:

- Take a block with multiple miner address that has transactions with fee > 0, 
then sum all the values of edges with target set to each miner address. The 
sum should be not be different than the value given in the block json retrieved 
from bitcoin-qt. 

- The sum of all the values of edges where targets are the miner addresses should 
equal sum of the all the fee in all the transactions plus the block mining reward. 

- Building the graph when the whole input is paid as fee (i.e., no output).

# A block that has a lot of chain transfers:

```
120000
```
