---
title: Hacker's Cookbook
description: Advanced Usage & Troubleshooting
sidebar_position: 9
---

### Making Bitcoin Core Accessible from Another Computer on the LAN {#bitcoin-core-lan}

Set the `bitcoin.conf` file as the following

```
rpcbind=127.0.0.1
rpcbind=192.168.1.2
rpcallowip=192.168.1.2
rpcallowip=192.168.1.3
debug=http
server=1
rest=1
txindex=1
rpcworkqueue=100
```

where `192.168.1.2` is the IP address of the computer where Bitcoin Core
is running, and `192.168.1.3` is the IP address of the computer where 
you want to query Bitcoin Core. 
Alternatively you can use `192.168.1.1/24`
to allow every computer in the subnet to query Bitcoin Core, 
at the cost of less restricted access.

To check if the node is accessible, 
on the host machine you can run the following to check if 
Bitcoin Core is correctly listening on the given port:

```bash
> netstat -aonq | findstr 8332
TCP    127.0.0.1:8332       0.0.0.0:0      LISTENING       64324
TCP    192.168.1.2:8332     0.0.0.0:0      LISTENING       64324
```

if the output of this command is at the following, it indicates that 
Bitcoin Core is listening on `localhost` and the port is not reachable 
from any other computer. 
In this case, make sure the above configuration
is set correctly, the `rpcbind` parameter specifically.

```bash
> netstat -aonq | findstr 8332
TCP    127.0.0.1:8332       0.0.0.0:0      LISTENING       2416
TCP    [::1]:8332           [::]:0         LISTENING       2416
```

On the client machine you can run the following to 
check if the port is open and accessible:

```bash
> nc -z 192.168.1.2 8332
Connection to 192.168.1.2 port 8332 (tcp/*) succeeded!
```

Read more about [networking](https://bitcoin.org/en/full-node#upgrading-bitcoin-core)
or related [security details](https://github.com/bitcoin/bitcoin/blob/master/doc/JSON-RPC-interface.md#security).



### Some blocks for debugging
- There are many transactions where they create many outputs 
with 0 value. For instance, the transaction with id 
`ceb1a7fb57ef8b75ac59b56dd859d5cb3ab5c31168aa55eb3819cd5ddbd3d806`
belonging to the block with height `123573`, contains `279` outputs
with value `0`. 

- Examples of some bad/strange transactions:
    - 71036 (search for txes in this block)
    - 268449
    - 565912
    - 706953
    - 774532
    - 710061 (some execution path related to this are currently not implemented)
