using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ShiftSoftware.ShiftEntity.Core.Services;
using System.Net;
using System.Text.Json;
using ShiftSoftware.ShiftEntity.Functions.FileExplorer;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace StockPlusPlus.Functions.Functions;

public class FileManager
{
    private AzureStorageService azureStorageService;

    public FileManager(AzureStorageService azureStorageService)
    {
        this.azureStorageService = azureStorageService;
    }


    [Function("zip")]
    public static ZipResponse Zip([HttpTrigger(AuthorizationLevel.Function, "POST")] HttpRequestData req,
        [Microsoft.Azure.Functions.Worker.Http.FromBody] ZipOptionsDTO dto,
        FunctionContext executionContext)
    {

        var logger = executionContext.GetLogger("zip");
        logger.LogInformation("zip function triggered.");

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        response.WriteStringAsync(JsonSerializer.Serialize(dto));

        // Return a response to both HTTP trigger and storage output binding.
        return new ZipResponse()
        {
            Messages = [dto],
            HttpResponse = response
        };

    }

    [Function("unzip")]
    public static UnzipResponse Unzip([HttpTrigger(AuthorizationLevel.Function, "POST")] HttpRequestData req,
        [Microsoft.Azure.Functions.Worker.Http.FromBody] ZipOptionsDTO dto,
        FunctionContext executionContext)
    {

        var logger = executionContext.GetLogger("unzip");
        logger.LogInformation("Unzip function triggered.");

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        response.WriteStringAsync(JsonSerializer.Serialize(dto));

        // Return a response to both HTTP trigger and storage output binding.
        return new UnzipResponse()
        {
            Messages = [dto],
            HttpResponse = response
        };

    }


    [Function("zip-queue")]
    public async Task<string[]> ZipQueue([QueueTrigger(Queues.Zip)] ZipOptionsDTO item,
        FunctionContext executionContext,
        string id,
        CancellationToken cancellationToken)
    {
        var logger = executionContext.GetLogger("zip-queue");
        logger.LogInformation($"zip-queue #{id} function triggered.");

        var FM = new ArchiveOperations(azureStorageService);
        await FM.ZipFiles(item, cancellationToken);

        string[] messages = { $"{item.Names.Count()} files zipped" };

        logger.LogInformation("{msg1}", messages[0]);

        // Queue Output messages
        return messages;
    }

    [Function("unzip-queue")]
    public async Task<string[]> UnzipQueue([QueueTrigger(Queues.Unzip)] ZipOptionsDTO item,
        FunctionContext executionContext,
        string id,
        CancellationToken cancellationToken)
    {
        var logger = executionContext.GetLogger("unzip-queue");
        logger.LogInformation($"unzip-queue function triggered. #{id}");

        var FM = new ArchiveOperations(azureStorageService);
        await FM.UnzipFiles(item, cancellationToken);

        string[] messages = { $"1 file unzipped: {item.Names.First()}" };

        logger.LogInformation("{msg1}", messages[0]);

        // Queue Output messages
        return messages;
    }
}