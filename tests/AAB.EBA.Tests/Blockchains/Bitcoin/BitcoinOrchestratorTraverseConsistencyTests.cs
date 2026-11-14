using AAB.EBA.Tests.TestKit.BitcoinCore;
using AAB.EBA.Tests.TestKit.Traverse;

using System.Linq;
using System.Threading.Tasks;

using Xunit;

namespace AAB.EBA.Tests.Blockchains.Bitcoin;

[Trait("Category", "Functional")]
public class BitcoinOrchestratorTraverseConsistencyTests : TestsBase
{
    [Theory]
    [InlineData(71036)]
    [InlineData(132317)]
    [InlineData(717531)]
    public async Task TraverseAsync_SingleBlock_MatchesRecordedBlock(int height)
    {
        // Arrange
        var block = RecordedBlock.FromMockData(height);
        var options = await RecordedTraverse.RunAsync(TempExeDir, height, height);

        // Act
        var output = await TraverseGraphOutput.ReadAsync(options);

        // Assert
        Assert.Multiple(
            () => AssertTransactionCounts(block, output),
            () => AssertTransactionBalances(block, output),
            () => AssertMintedValue(block, output));
    }

    private static void AssertTransactionCounts(RecordedBlock block, TraverseGraphOutput output)
    {
        Assert.All(block.Transactions, tx =>
        {
            Assert.Equal(tx.InputCount, output["edges_Script_Redeems_Tx"].Where(":END_ID(Tx)", tx.Id).Rows.Count);
            Assert.Equal(tx.OutputCount, output["edges_Tx_Credits_Script"].Where(":START_ID(Tx)", tx.Id).Rows.Count);
            Assert.Single(output["edges_Block_Confirms_Tx"].Where(":END_ID(Tx)", tx.Id).Rows);
        });
        Assert.Equal(block.Transactions.Count, output["edges_Block_Confirms_Tx"].Rows.Count);
        Assert.Equal(block.Transactions.Sum(x => x.InputCount), output["edges_Script_Redeems_Tx"].Rows.Count);
        Assert.Equal(block.Transactions.Sum(x => x.OutputCount), output["edges_Tx_Credits_Script"].Rows.Count);
    }

    private static void AssertTransactionBalances(RecordedBlock block, TraverseGraphOutput output)
    {
        Assert.All(block.Transactions.Where(x => !x.IsCoinbase), tx =>
        {
            Assert.Equal(tx.InputValue, output["edges_Script_Redeems_Tx"].Where(":END_ID(Tx)", tx.Id).Sum("Value"));
            Assert.Equal(tx.OutputValue, output["edges_Tx_Credits_Script"].Where(":START_ID(Tx)", tx.Id).Sum("Value"));
            var fee = output["edges_Tx_Fee_Tx"].Where(":START_ID(Tx)", tx.Id);
            Assert.Single(fee.Rows);
            Assert.Equal(tx.Fee, fee.Sum("Value"));
            Assert.Equal(tx.InputValue - tx.OutputValue, fee.Sum("Value"));
            Assert.Equal(block.Coinbase.Id, fee.Rows[0][fee.Column(":END_ID(Tx)")]);
        });
    }

    private static void AssertMintedValue(RecordedBlock block, TraverseGraphOutput output)
    {
        var mint = output["edges_Coinbase_Mints_Tx"];
        Assert.Single(mint.Rows);
        Assert.Equal(block.Coinbase.Id, mint.Rows[0][mint.Column(":END_ID(Tx)")]);
        Assert.Equal(block.Coinbase.OutputValue - block.Transactions.Sum(x => x.Fee), mint.Sum("Value"));
        Assert.Equal(5_000_000_000L >> (int)(block.Height / 210_000), mint.Sum("Value"));
    }
}
