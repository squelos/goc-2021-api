using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Services.Interfaces;
using SnapZen.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnapZen.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DiscussionController : ControllerBase
    {
        private readonly ILogger<ConnectionController> _logger;
        private readonly IBlobService blobService;
        private readonly IConfiguration Configuration;

        public DiscussionController(ILogger<ConnectionController> logger, IBlobService blobService, IConfiguration configuration)
        {
            _logger = logger;
            this.blobService = blobService;
            this.Configuration = configuration;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost("pushImage")]
        public async Task<bool> pushImage(ImageInput input)
        {
            var config = this.Configuration.GetSection("Key")["containerName"];
            try
            {
                var blob = blobService.GetBlob("sessionGuid", config);
                if(blob != null)
                {
                    var fileName = input.user.Id + "/" + input.user.Id + "-" + input.sessionGuid + "/" + input.imageName;
                    await blobService.UploadFileBlob(fileName, input.image, config);
                }

                return true;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
                return false;
            }

        }
    }
}
