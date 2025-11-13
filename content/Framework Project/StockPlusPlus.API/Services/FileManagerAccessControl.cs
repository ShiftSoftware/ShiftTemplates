using Azure.Storage.Blobs;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Model.FileExplorer.Dtos;
using ShiftSoftware.ShiftEntity.Web.Services;
using System.Text.RegularExpressions;

namespace StockPlusPlus.API.Services;

public class FileManagerAccessControl : IFileExplorerAccessControl
{
    readonly string permissions = "/Extra/Downloads|/Extra/{dealer_name} Downloads/{branch_name}|/Extra/{dealer_name} Downloads/Shared|/Extra/TOS/{dealer_name}/{branch_name}|/Extra/TOS/{dealer_name}/Shared|/Extra/TOS/Shared|/Extra/Business Report/{dealer_name}|/Extra/Business Report/Shared|/Extra/Best Practices/{dealer_name}|/Extra/Best Practices/Shared";
    
    public async Task<IEnumerable<string>> FilterWithReadAccessAsync(BlobContainerClient container, IEnumerable<string> files)
    {
        var newDetails = new List<string>();

        foreach (var file in files)
        {
            if (file == null)
                continue;
            var permission = UserCanRead_WritePath(file, permissions, permissions, permissions, "Shift Software - HQ", "Shift Software", []);

            if (!permission.Read)
                continue;

            newDetails.Add(file);
        }

        foreach (var item in files.Where(x => x?.EndsWith("info.deleted") == true).ToList())
        {
            newDetails.Remove(item);

            //Read content of the file
            var blob = container.GetBlobClient(item);

            var content = await blob.DownloadContentAsync();

            var contentString = content.Value.Content.ToString();

            var deletedPaths = contentString.Split("\r\n").ToList().Where(x => !string.IsNullOrWhiteSpace(x));

            foreach (var deletedPath in deletedPaths)
            {
                var deletedItem = newDetails.FirstOrDefault(x => x?.StartsWith(deletedPath) == true || $"/{x}".StartsWith(deletedPath));

                if (deletedItem != null)
                {
                    newDetails.Remove(deletedItem);
                }
            }
        }

        return newDetails;
    }

    public IEnumerable<string> FilterWithWriteAccess(IEnumerable<string> files)
    {
        var newFiles = new List<string>();

        foreach (var item in files)
        {
            if (!item!.StartsWith("Extra/"))
            {
                newFiles.Add(item);
                continue;
            }

            var permission = UserCanRead_WritePath(item!, permissions, permissions, permissions, "Shift Software - HQ", "Shift Software", []);

            if (!permission.Write)
                continue;

            newFiles.Add(item);
        }

        return newFiles;
    }

    public IEnumerable<string> FilterWithDeleteAccess(IEnumerable<string> data)
    {
        var newDirectoryContent = new List<string>();

        foreach (var item in data)
        {
            if (item == null)
                continue;

            var permission = UserCanRead_WritePath(item, permissions, permissions, permissions, "Shift Software - HQ", "Shift Software", []);

            if (!permission.Remove)
                continue;

            newDirectoryContent.Add(item);
        }

        return newDirectoryContent;
    }

    public class DownloadableFileAccess
    {
        public bool Read { get; set; }
        public bool Write { get; set; }
        public bool Remove { get; set; }
        public DownloadableFileAccess(bool read, bool write, bool remove)
        {
            this.Read = read;
            this.Write = write;
            this.Remove = remove;
        }
    }
    public static DownloadableFileAccess UserCanRead_WritePath(string path, string readAccessPaths, string writeAccessPaths, string removeAccessPaths, string userBranchName, string divisionName, List<string> deletedPaths)
    {
        string shouldStartWith = "Extra";
        path = Regex.Replace(path, "/+", "/");
        path = path.TrimEnd('/');
        var readAccess = false;
        var writeAccess = false;
        var removeAccess = false;
        //var absolutePath = HostingEnvironment.MapPath("/" + path);
        if (
                (path.StartsWith(shouldStartWith) || path.StartsWith($"/{shouldStartWith}"))
            //&& !ForbiddenPaths.Any(y => path.EndsWith(y))
            //&& !(deletedPaths.Contains(absolutePath) && !ShiftSoftware.AuthorizationServer.AllPermissions.FileSystemPermissions.CanSeeRemovedFiles(loggedInUser))
            )
        {
            //var readAccessPaths = ShiftSoftware.AuthorizationServer.AllPermissions.FileSystemPermissions.ReadAccessPaths(loggedInUser);
            //var writeAccessPaths = ShiftSoftware.AuthorizationServer.AllPermissions.FileSystemPermissions.WriteAccessPaths(loggedInUser);
            //var removeAccessPaths = ShiftSoftware.AuthorizationServer.AllPermissions.FileSystemPermissions.RemoveAccessPaths(loggedInUser);
            readAccessPaths = readAccessPaths.Replace("{dealer_name}", divisionName);
            writeAccessPaths = writeAccessPaths.Replace("{dealer_name}", divisionName);
            removeAccessPaths = removeAccessPaths.Replace("{dealer_name}", divisionName);
            readAccessPaths = readAccessPaths.Replace("{branch_name}", userBranchName);
            writeAccessPaths = writeAccessPaths.Replace("{branch_name}", userBranchName);
            removeAccessPaths = removeAccessPaths.Replace("{branch_name}", userBranchName);
            foreach (var accessiblePath in readAccessPaths.Split('|'))
            {
                if (!string.IsNullOrEmpty(accessiblePath) && (path.StartsWith(accessiblePath) || ("/" + path).StartsWith(accessiblePath)) || readAccessPaths.Split('|').Any(x => x.StartsWith("/" + path + "/") || x.StartsWith(path + "/")))
                {
                    readAccess = true;
                    break;
                }
            }
            foreach (var writablePath in writeAccessPaths.Split('|'))
            {
                if (!string.IsNullOrEmpty(writablePath) && (path.StartsWith(writablePath) || ("/" + path).StartsWith(writablePath)))
                {
                    writeAccess = true;
                    break;
                }
            }
            foreach (var removablePath in removeAccessPaths.Split('|'))
            {
                if (!string.IsNullOrEmpty(removablePath) && (path.StartsWith(removablePath) || ("/" + path).StartsWith(removablePath)))
                {
                    removeAccess = true;
                    break;
                }
            }
        }
        return new DownloadableFileAccess(readAccess, writeAccess, removeAccess);
    }
}