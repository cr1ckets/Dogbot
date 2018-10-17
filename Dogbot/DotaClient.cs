using System;
using System.Collections.Generic;
using System.Threading;

using SteamKit2;
using SteamKit2.Internal;
using SteamKit2.GC;
using SteamKit2.GC.Dota.Internal;

namespace DotaMatchRequest
{
    class DotaClient
    {
        SteamClient client;

        SteamUser user;
        SteamGameCoordinator gameCoordinator;

        CallbackManager callbackMgr;


        string userName;
        string password;

        uint matchId;

        bool gotMatch;


        public CMsgDOTAMatch Match { get; private set; }


        const int APPID = 570;


        public DotaClient(string userName, string password, uint matchId)
        {
            this.userName = userName;
            this.password = password;

            this.matchId = matchId;

            client = new SteamClient();

            user = client.GetHandler<SteamUser>();
            gameCoordinator = client.GetHandler<SteamGameCoordinator>();

            callbackMgr = new CallbackManager(client);

            callbackMgr.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            callbackMgr.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            callbackMgr.Subscribe<SteamGameCoordinator.MessageCallback>(OnGCMessage);
        }


        public void Connect()
        {
            Console.WriteLine("Connecting to Steam...");

            client.Connect();
        }

        public void Wait()
        {
            while (!gotMatch)
            {
                callbackMgr.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }
        }


        void OnConnected(SteamClient.ConnectedCallback callback)
        {
            Console.WriteLine("Connected! Logging '{0}' into Steam...", userName);

            user.LogOn(new SteamUser.LogOnDetails
            {
                Username = userName,
                Password = password,
            });
        }

        void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            if (callback.Result != EResult.OK)
            {


                Console.WriteLine("Unable to logon to Steam: {0}", callback.Result);

                gotMatch = true; 
                return;
            }

            Console.WriteLine("Logged in! Launching DOTA...");



            var playGame = new ClientMsgProtobuf<CMsgClientGamesPlayed>(EMsg.ClientGamesPlayed);

            playGame.Body.games_played.Add(new CMsgClientGamesPlayed.GamePlayed
            {
                game_id = new GameID(APPID),
            });


            client.Send(playGame);

            Thread.Sleep(5000);

            var clientHello = new ClientGCMsgProtobuf<CMsgClientHello>((uint)EGCBaseClientMsg.k_EMsgGCClientHello);
            clientHello.Body.engine = ESourceEngine.k_ESE_Source2;
            gameCoordinator.Send(clientHello, APPID);
        }


        void OnGCMessage(SteamGameCoordinator.MessageCallback callback)
        {

            var messageMap = new Dictionary<uint, Action<IPacketGCMsg>>
            {
                { ( uint )EGCBaseClientMsg.k_EMsgGCClientWelcome, OnClientWelcome },
                { ( uint )EDOTAGCMsg.k_EMsgGCMatchDetailsResponse, OnMatchDetails },
            };

            Action<IPacketGCMsg> func;
            if (!messageMap.TryGetValue(callback.EMsg, out func))
            {

                return;
            }

            func(callback.Message);
        }


        void OnClientWelcome(IPacketGCMsg packetMsg)
        {
            
            var msg = new ClientGCMsgProtobuf<CMsgClientWelcome>(packetMsg);

            Console.WriteLine("GC is welcoming us. Version: {0}", msg.Body.version);

            Console.WriteLine("Requesting details of match {0}", matchId);


            var requestMatch = new ClientGCMsgProtobuf<CMsgGCMatchDetailsRequest>((uint)EDOTAGCMsg.k_EMsgGCMatchDetailsRequest);
            requestMatch.Body.match_id = matchId;

            gameCoordinator.Send(requestMatch, APPID);
        }

        void OnMatchDetails(IPacketGCMsg packetMsg)
        {
            var msg = new ClientGCMsgProtobuf<CMsgGCMatchDetailsResponse>(packetMsg);

            EResult result = (EResult)msg.Body.result;
            if (result != EResult.OK)
            {
                Console.WriteLine("Unable to request match details: {0}", result);
            }

            gotMatch = true;
            Match = msg.Body.match;

            client.Disconnect();
        }


        static string GetEMsgDisplayString(uint eMsg)
        {
            Type[] eMsgEnums =
            {
                typeof( EGCBaseClientMsg ),
                typeof( EDOTAGCMsg ),
                typeof( EGCBaseMsg ),
                typeof( EGCItemMsg ),
                typeof( ESOMsg ),
                typeof( EGCSystemMsg ),
            };

            foreach (var enumType in eMsgEnums)
            {
                if (Enum.IsDefined(enumType, (int)eMsg))
                    return Enum.GetName(enumType, (int)eMsg);

            }

            return eMsg.ToString();
        }
    }
}
