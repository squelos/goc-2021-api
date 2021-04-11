using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.MixedReality.WebRTC;
using Newtonsoft.Json;
using Services.Interfaces;
using SnapZen.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnapZen.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ConnectionController : ControllerBase
    {

        private readonly ILogger<ConnectionController> _logger;
        private readonly IBlobService blobService;
        private readonly IConfiguration Configuration;
        private IMemoryCache _cache;

        public ConnectionController(ILogger<ConnectionController> logger, IBlobService blobService, IConfiguration configuration, IMemoryCache memoryCache)
        {
            _logger = logger;
            this.blobService = blobService;
            this.Configuration = configuration;
            _cache = memoryCache;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpGet("firstConnect")]
        public async Task<User> FirstConnect(string name)
        {
            var config = this.Configuration.GetSection("Key")["containerName"];
            var user = new User();
            user.Id = Guid.NewGuid();
            user.DisplayName = name;

            var fileName = user.Id + "/" + user.Id + "-" + DateTime.Now.ToFileTime().ToString() + ".json";
            await blobService.UploadFileBlob(fileName, user, config);

            return user;
        }      
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet("createSessionId")]
        public async Task<Session> getSessionId()
        {
            var randomizer = new Random();
            var sessionId = randomizer.Next(1000000);
            var session = new Session { SessionId = sessionId };
            _cache.Set("tempSession" + DateTime.Now.ToShortDateString().ToString()+ sessionId, sessionId, DateTime.Now.AddSeconds(300));
            return session;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpPost("storeSDPInfo")]
        public async Task<Session> storeSDPInfo(Session session)
        {
            var randomizer = new Random();
            var sessionId = randomizer.Next(1000000);
            _cache.Set("spdInfo" + DateTime.Now.ToShortDateString().ToString() + sessionId, session, DateTime.Now.AddSeconds(300));
            return session;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        [HttpPost("inputSessionId")]
        public async Task<Session> InputSessionId(User user, int sessionId)
        {
            var config = this.Configuration.GetSection("Key")["containerName"];
            var session = new SessionGuid();
            var sessionInfo = new Session();
            try
            {
                var data = _cache.Get("tempSession" + DateTime.Now.ToShortDateString().ToString() + sessionId);
                if (data == null)
                    throw new Exception("Invalid Code");

                session.SessionId = Guid.NewGuid();
                var fileName = user.Id + "/" + user.Id + "-" + session.SessionId + "/" + DateTime.Now.ToFileTime().ToString() + ".json";
                await blobService.UploadFileBlob(fileName, user, config);

                sessionInfo = (Session)(_cache.Get("spdInfo" + DateTime.Now.ToShortDateString().ToString() + sessionId));
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return sessionInfo;

        }

        [HttpGet("initiateCallRTC")]
        public async Task<string> InitiateCallRTC()
        {
            var list = new List<string>();
            list.Add(this.Configuration.GetSection("Key")["iceServer"]); 
            AudioTrackSource microphoneSource = null;
            LocalAudioTrack localAudioTrack = null;
            Transceiver audioTransceiver = null;

            var iceServer = new IceServer
            {
                Urls = list,
                TurnPassword = this.Configuration.GetSection("Key")["turnPwd"],
                TurnUserName = this.Configuration.GetSection("Key")["turnUser"]
            };

            var serverList = new List<IceServer>();
            serverList.Add(iceServer);
            var connectionConfig = new PeerConnectionConfiguration { 
                IceServers = serverList,
                IceTransportType = IceTransportType.All,
                BundlePolicy = BundlePolicy.Balanced,
                SdpSemantic = SdpSemantic.UnifiedPlan
            };
            var connection = new PeerConnection();
            await connection.InitializeAsync(connectionConfig);
            microphoneSource = await DeviceAudioTrackSource.CreateAsync();
            var audioTrackConfig = new LocalAudioTrackInitConfig
            {
                trackName = "microphone_track"
            };
            localAudioTrack = LocalAudioTrack.CreateFromSource(microphoneSource, audioTrackConfig);

            audioTransceiver = connection.AddTransceiver(MediaKind.Audio);
            audioTransceiver.LocalAudioTrack = localAudioTrack;
            audioTransceiver.DesiredDirection = Transceiver.Direction.SendReceive;

            var signaler = new NamedPipeSignaler.NamedPipeSignaler(connection, "testpipe");

            connection.Connected += () => {
                Console.WriteLine("PeerConnection: connected.");
            };

            signaler.SdpMessageReceived += async (SdpMessage message) =>
            {
                // Note: we use 'await' to ensure the remote description is applied
                // before calling CreateAnswer(). Failing to do so will prevent the
                // answer from being generated, and the connection from establishing.
                await connection.SetRemoteDescriptionAsync(message);
                if (message.Type == SdpMessageType.Offer)
                {
                    connection.CreateAnswer();
                }
            };

            await signaler.StartAsync();

            signaler.IceCandidateReceived += (IceCandidate candidate) => {
                connection.AddIceCandidate(candidate);
            };

            connection.IceStateChanged += (IceConnectionState newState) => {
                Console.WriteLine($"ICE state: {newState}");
            };

            if (signaler.IsClient)
            {
                Console.WriteLine("Connecting to remote peer...");
                connection.CreateOffer();
            }
            else
            {
                Console.WriteLine("Waiting for offer from remote peer...");
            }

            return connection.IsConnected + "-" + connection.Name + "-" ;

        }
    }
}
