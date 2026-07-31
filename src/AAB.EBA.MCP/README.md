```shell
# in 1st terminal, start the mcp service:
dotnet .\src\AAB.EBA.MCP\bin\Debug\net10.0\AAB.EBA.MCP.dll

# in 2nd terminal, run inspector:
npx @modelcontextprotocol/inspector
```

After the inspector webpage opens: 

- Add server entry
    - Transport: Streamable HTTP
    - URL: http://localhost:5000
