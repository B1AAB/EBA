
Note that the expected data are generated using EBA; 
hence, before generating new expected data, make sure 
EBA is running as expected. Or double-check the generated
output and make sure it contains data as expected. 
Otherwise, if there is any bug in EBA processing
any particular block/tx, that bug will be present 
in the generated expected data, hence the tests will
pass. 


## Blocks-only recordings

With a REST-enabled Bitcoin Core at `http://127.0.0.1:8332/rest/`, run
`dotnet run --project tests/AAB.EBA.TestDataGenerator -- api_mock_blocks 71037-71037,132317-132317,717531-717531`.
This adds block JSON and height-to-hash mappings without downloading `tx_*.json`
or replacing the existing chain-info recording. The older `api_mock` and `all`
commands continue to record individual transactions as well.

The tests read sorted TSV baselines from `ExpectedOutputs/Traverse/{height}`.
The generator's legacy `all` command still writes range-named output directories;
those are not the baselines used by the tests. Use the Debug-only update procedure
in `tests/AAB.EBA.Tests/README.md` to update the TSV baselines.

good blocks for testing:
700000-700005
300000-300001
700000-700001
714460-714461
100-101
120460-120464
