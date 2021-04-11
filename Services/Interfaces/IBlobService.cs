using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IBlobService
    {
        Task<IEnumerable<string>> GetAllBlobs(string containerName);
        Task<string> GetBlob(string name, string containerName);
        Task<bool> UploadFileBlob(string name, Object data, string containerName);

    }
}
