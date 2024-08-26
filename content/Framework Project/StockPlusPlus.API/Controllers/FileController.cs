using Azure.Storage.Sas;
using Microsoft.AspNetCore.Mvc;
using ShiftSoftware.ShiftEntity.Core.Services;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.TypeAuth.AspNetCore;
using ShiftSoftware.TypeAuth.Core;
using StockPlusPlus.Shared.ActionTrees;
using System.Drawing;
using System.Text.Json;

namespace StockPlusPlus.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private AzureStorageService azureStorageService;
        private readonly ITypeAuthService typeAuthService;

        public FileController(AzureStorageService azureStorageService, ITypeAuthService typeAuthService)
        {
            this.azureStorageService = azureStorageService;
            this.typeAuthService = typeAuthService;
        }

        //[HttpPost("upload")]
        //[TypeAuth(typeof(SystemActionTrees), nameof(SystemActionTrees.UploadFiles), Access.Write)]
        //public async Task<IActionResult> UploadAsync(IFormFile file, [FromHeader(Name = "Account-Name")] string? AccountName, [FromHeader(Name = "Container-Name")] string? ContainerName)
        //{
        //    var res = new ShiftEntityResponse<ShiftFileDTO>();

        //    var maxUploadSize = typeAuthService.AccessValue(SystemActionTrees.MaxUploadSizeInMegaBytes);

        //    if (file.Length / 1024m / 1024m > maxUploadSize)
        //    {
        //        res.Message = new Message { Title = "Error", Body = $"Maximum upload size of {maxUploadSize} has exceeded" };

        //        return Ok(res);
        //    }


        //    string? blob;

        //    AccountName = AccountName ?? azureStorageService.GetDefaultAccountName();
        //    ContainerName = ContainerName ?? azureStorageService.GetDefaultContainerName(AccountName);

        //    try
        //    {
        //        Stream s = new MemoryStream();

        //        await file.CopyToAsync(s);

        //        blob = await azureStorageService.UploadAsync(file.Name, s, ContainerName, AccountName);

        //        if (blob == null) throw new Exception("Could not retrieve blob name");
        //    }
        //    catch (Exception e)
        //    {
        //        res.Message = new Message { Title = "Failed to upload file to the cloud", Body = e.Message };
        //        return Ok(res);
        //    }

        //    res.Entity = new ShiftFileDTO
        //    {
        //        Name = file.FileName,
        //        Blob = blob,
        //        ContentType = file.ContentType,
        //        Size = file.Length,
        //        AccountName = AccountName,
        //        ContainerName = ContainerName,
        //    };

        //    try
        //    {
        //        if (file.ContentType.StartsWith("image"))
        //        {
        //            var memoryStream = new MemoryStream();
        //            file.CopyTo(memoryStream);
        //            Image image = Image.FromStream(memoryStream);

        //            res.Entity.Width = image.Width;
        //            res.Entity.Height = image.Height;
        //        }
        //    }
        //    catch
        //    {
        //        res.Message = new Message { Title = "Could not parse image" };
        //    }

        //    return Ok(res);
        //}

        //[HttpPost("generate-file-upload-sas")]
        //[TypeAuth(typeof(SystemActionTrees), nameof(SystemActionTrees.UploadFiles), Access.Write)]
        //public ActionResult<ShiftEntityResponse<List<ShiftFileDTO>>> GenerateFileUploadSAS([FromBody] List<ShiftFileDTO> files)
        //{
        //    var res = new ShiftEntityResponse<List<ShiftFileDTO>>();

        //    foreach (var file in files)
        //    {
        //        var AccountName = file.AccountName ?? azureStorageService.GetDefaultAccountName();

        //        var ContainerName = file.ContainerName ?? azureStorageService.GetDefaultContainerName(AccountName);

        //        file.Url = this.azureStorageService.GetSignedURL(file.Blob!, BlobSasPermissions.Write | BlobSasPermissions.Read, ContainerName, AccountName, 60);
        //    }

        //    res.Entity = files;

        //    return new ContentResult()
        //    {
        //        Content = JsonSerializer.Serialize(res, new JsonSerializerOptions { }),
        //        ContentType = "application/json"
        //    };
        //}
    }
}
