using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using StackExchange.Redis;
using System.Text.Json;

Console.WriteLine("***** Geracao de cotacoes de moedas estrangeiras para testes com Redis *****");
Console.WriteLine();

const decimal VALOR_BASE_DOLAR = 5.161m;
const decimal VALOR_BASE_EURO = 5.972m;
const decimal VALOR_BASE_LIBRA = 6.976m;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .Build();

var logger = new LoggerConfiguration()
    .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
    .CreateLogger();

var redisConnection = ConnectionMultiplexer.Connect(
    configuration.GetConnectionString("Redis")!);
var expirationSeconds = Convert.ToInt32(configuration["ExpirationInSeconds"]!);
while (true)
{
    var dbRedis = redisConnection.GetDatabase();

    logger.Information("Gerando novas cotacoes de moedas estrangeiras...");
    var dolar = VALOR_BASE_DOLAR + new Random().Next(0, 21) / 1000m;
    var euro = VALOR_BASE_EURO + new Random().Next(0, 22) / 1000m;
    var libra = VALOR_BASE_LIBRA + new Random().Next(0, 23) / 1000m;
    dbRedis.HashSet("SimulacaoCotacoes", [
            new HashEntry("UltimaAtualizacao", JsonSerializer.Serialize(DateTime.UtcNow.AddHours(-3))),
            new HashEntry("Dolar", JsonSerializer.Serialize(dolar)),
            new HashEntry("Euro", JsonSerializer.Serialize(euro)),
            new HashEntry("Libra", JsonSerializer.Serialize(libra))
        ]);
    logger.Information("Novas cotacoes de moedas estrangeiras geradas e armazenadas no Redis com sucesso!");
    if (expirationSeconds > 0)
    {
        dbRedis.KeyExpire("SimulacaoCotacoes", TimeSpan.FromSeconds(expirationSeconds));
        logger.Information($"As cotacoes de moedas estrangeiras expiram em {expirationSeconds} segundos...");
    }

    logger.Information("Consultando as cotacoes geradas...");
    var hashEntries = dbRedis.HashGetAll("SimulacaoCotacoes");
    var dict = hashEntries.ToDictionary(
        he => he.Name.ToString(),
        he => he.Value.ToString());
    logger.Information($"Cotacoes atuais: {JsonSerializer.Serialize(dict)}");

    Console.WriteLine("Pressione ENTER para gerar novas cotacoes ou CTRL+C para sair...");
    Console.ReadLine();
}