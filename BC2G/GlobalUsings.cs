global using BC2G.Blockchains.Bitcoin;
global using BC2G.Blockchains.Bitcoin.Graph;
global using BC2G.Blockchains.Bitcoin.Model;
global using BC2G.CommandLineInterface;
global using BC2G.Graph.Db;
global using BC2G.Graph.Model;
global using BC2G.Infrastructure;
global using BC2G.Infrastructure.StartupSolutions;
global using BC2G.Model.Config;
global using BC2G.PersistentObject;
global using BC2G.Serializers;

global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;

global using Neo4j.Driver;

global using Npgsql;

global using Polly;
global using Polly.Contrib.WaitAndRetry;
global using Polly.Extensions.Http;
global using Polly.Timeout;
global using Polly.Wrap;

global using Serilog;
global using Serilog.Sinks.SystemConsole.Themes;

global using System.Collections.Concurrent;
global using System.Collections.ObjectModel;
global using System.CommandLine;
global using System.CommandLine.Binding;
global using System.CommandLine.Builder;
global using System.CommandLine.Invocation;
global using System.CommandLine.Parsing;
global using System.ComponentModel.DataAnnotations;
global using System.ComponentModel.DataAnnotations.Schema;
global using System.Diagnostics;
global using System.Net;
global using System.Security.Cryptography;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Text.RegularExpressions;
