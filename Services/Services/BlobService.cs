using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Newtonsoft.Json;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Services.Services
{
    /// <summary>
    /// This service is used to upload the data generated into the proper sub domain cache
    /// </summary>
    public class BlobService : IBlobService
    {
        private readonly BlobServiceClient _blobClient;

        public BlobService(BlobServiceClient blobClient)
        {
            _blobClient = blobClient;
        }

        /// <summary>
        /// Get all the files as blob inside a specified container
        /// </summary>
        /// <param name="containerName">the container name for which we eant all the files</param>
        /// <returns>a file list</returns>
        public async Task<IEnumerable<string>> GetAllBlobs(string containerName)
        {
            var containerClient = _blobClient.GetBlobContainerClient(containerName);
            var files = new List<string>();

            var blobs = containerClient.GetBlobsAsync(); 
            
            await foreach (var item in blobs)
            {
                files.Add(item.Name);
            }

            return files;
        }

        /// <summary>
        /// Get the blob storage container
        /// </summary>
        /// <param name="name">name of the container</param>
        /// <param name="containerName">name of the azure file storage global container</param>
        /// <returns></returns>
        public async Task<string> GetBlob(string name, string containerName)
        {
            var containerClient = _blobClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(name);

            var str = blobClient.Uri.AbsoluteUri;

            return str;
        }

        /// <summary>
        /// Upload the data from a json stream directly into the azure blob storage
        /// </summary>
        /// <param name="name">the container name</param>
        /// <param name="data">the json data to upload</param>
        /// <param name="containerName">name of the azure file storage global container</param>
        /// <returns></returns>
        public async Task<bool> UploadFileBlob(string name, Object data, string containerName)
        {
            try
            {
                var containerClient = _blobClient.GetBlobContainerClient(containerName);

                var blobClient = containerClient.GetBlobClient(name);

                var httpHeaders = new BlobHttpHeaders()
                {
                    ContentType = "application/json"
                };

                using(var ms = new MemoryStream())
                {
                    var json = JsonConvert.SerializeObject(data);
                    StreamWriter writer = new StreamWriter(ms);
                    writer.Write(json);
                    writer.Flush();
                    ms.Position = 0;

                    await blobClient.UploadAsync(ms, httpHeaders);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return false;
        }
    }
}
