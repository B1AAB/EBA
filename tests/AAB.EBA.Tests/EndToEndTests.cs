using AAB.EBA.Blockchains.Bitcoin;

using Microsoft.Extensions.Logging.Abstractions;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AAB.EBA.Tests;

public class EndToEndTests : TestsBase, IClassFixture<ClientFixture>
{
    private readonly HttpClient _client;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly CancellationToken _cancellationToken;
    private readonly ClientFixture _clientFixture;
    private readonly string _mockDataBasePath = ClientFixture.BitcoinExpectedOutputsDir;

    public EndToEndTests(ClientFixture clientFixture)
    {
        _client = clientFixture.Client;
        _clientFixture = clientFixture;

        _cancellationTokenSource = new CancellationTokenSource();
        _cancellationToken = _cancellationTokenSource.Token;
    }

    [Theory]
    [InlineData(71036, "00000000000997f9fd2fe1ee376293ef8c42ad09193a5d2086dddf8e5c426b56")]
    public async Task CanGetBlockHash(int blockHeight, string expectedBlockHash)
    {
        // Arrange
        var logger = NullLogger<BitcoinChainAgent>.Instance;
        var agent = new BitcoinChainAgent(_client, logger);

        // Act
        var blockHash = await agent.GetBlockHashAsync(blockHeight, _cancellationToken);

        // Assert
        Assert.False(string.IsNullOrEmpty(blockHash));
        Assert.Equal(expectedBlockHash, blockHash);
    }

    [Theory]
    [InlineData(71036)]
    public async Task CanGetAndDeserializeBlock(int blockHeight)
    {
        // Arrange
        var logger = NullLogger<BitcoinChainAgent>.Instance;
        var agent = new BitcoinChainAgent(_client, logger);

        // Act
        var blockHash = await agent.GetBlockHashAsync(blockHeight, _cancellationToken);
        var block = await agent.GetBlockAsync(blockHash, _cancellationToken);

        // Assert
        Assert.NotNull(block);
        Assert.Equal(blockHeight, block.Height);
        Assert.Equal(blockHash, block.Hash);
        Assert.NotEmpty(block.Transactions);
    }

    [Theory]
    [InlineData(71036)]
    public async Task BlockHasCoinbaseTransaction(int blockHeight)
    {
        // Arrange
        var logger = NullLogger<BitcoinChainAgent>.Instance;
        var agent = new BitcoinChainAgent(_client, logger);

        // Act
        var blockHash = await agent.GetBlockHashAsync(blockHeight, _cancellationToken);
        var block = await agent.GetBlockAsync(blockHash, _cancellationToken);

        // Assert
        Assert.Contains(block.Transactions, tx => tx.IsCoinbase);
    }

    [Theory]
    [InlineData(71036)]
    public async Task CanGetReferencedTransactions(int blockHeight)
    {
        // Arrange
        var logger = NullLogger<BitcoinChainAgent>.Instance;
        var agent = new BitcoinChainAgent(_client, logger);

        var blockHash = await agent.GetBlockHashAsync(blockHeight, _cancellationToken);
        var block = await agent.GetBlockAsync(blockHash, _cancellationToken);

        // Act & Assert
        // For non-coinbase transactions, verify we can fetch referenced input transactions
        // Only test with input txids that are also defined in this block (i.e., have mock data)
        var blockTxIds = block.Transactions
            .Where(tx => !string.IsNullOrEmpty(tx.Txid))
            .Select(tx => tx.Txid)
            .ToHashSet();

        var nonCoinbaseTx = block.Transactions.FirstOrDefault(tx => !tx.IsCoinbase);
        if (nonCoinbaseTx != null)
        {
            var inputWithTxid = nonCoinbaseTx.Inputs
                .FirstOrDefault(i => !string.IsNullOrEmpty(i.Txid) && blockTxIds.Contains(i.Txid));

            if (inputWithTxid != null)
            {
                var referencedTx = await agent.GetTransactionAsync(
                    inputWithTxid.Txid, _cancellationToken);

                Assert.NotNull(referencedTx);
                Assert.Equal(inputWithTxid.Txid, referencedTx.Txid);
            }
        }
    }
}
