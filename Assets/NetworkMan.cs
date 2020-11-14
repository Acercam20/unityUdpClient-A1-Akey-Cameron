using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;
using UnityEngine.UIElements;
using System.Linq;

public class NetworkMan : MonoBehaviour
{
    public UdpClient udp;
    private string IncomingMessage;
    public string myID = "newID";
    public GameObject PlayerPrefab;
    ///Too confusing must use long names. 
    public List<string> PlayersToSpawnList;
    public List<string> PlayersToDestroyList;
    public List<PlayerCube> PlayersInGameList;

    void Start()
    {


        udp = new UdpClient();

        udp.Connect("3.19.54.254", 12345);

        Byte[] sendBytes = Encoding.ASCII.GetBytes("connect");

        udp.Send(sendBytes, sendBytes.Length);

        udp.BeginReceive(new AsyncCallback(OnReceived), udp);

        InvokeRepeating("HeartBeat", 1, 1);

        //InvokeRepeating("SendStuff", 1, .02f);

    }

    void OnDestroy()
    {
        udp.Dispose();
    }


    public enum commands
    {
        NEW_CLIENT,
        UPDATE,
        CLIENT_REMOVED,
        GET_PLAYERS_IN_GAME,

    };

    [Serializable]
    public class Message
    {
        public commands cmd;

    }

    [Serializable]
    public class PlayerPosition 
    { 
        public Vector3 position;
    }

    [Serializable]
    public class Player
    {
        [Serializable]
        public struct receivedColor { public float R, G, B; }
        [Serializable]
        public struct receivedPosition { public float x, y, z; }
        public string id;
        public receivedColor color;
        public receivedPosition position;
    }

    [Serializable]
    public class NewPlayer 
    { 
        public Player player; 
    }

    [Serializable]
    public class DroppedPlayer 
    { 
        public string id; 
    }

    [Serializable]
    public class PlayersIG 
    { 
        public Player[] players; 
    }

    [Serializable]
    public class GameState 
    { 
        public Player[] players; 
    }

    public Message latestMessage; 
    public GameState latestGameState;

    void OnReceived(IAsyncResult result)
    {
        // this is what had been passed into BeginReceive as the second parameter:
        UdpClient socket = result.AsyncState as UdpClient;

        // points towards whoever had sent the message:
        IPEndPoint source = new IPEndPoint(0, 0);

        // get the actual message and fill out the source:
        byte[] message = socket.EndReceive(result, ref source);

        // do what you'd like with `message` here:
        string returnData = Encoding.ASCII.GetString(message);
        IncomingMessage = "Got this" + returnData;
        //Debug.Log("Got this: " + returnData);

        latestMessage = JsonUtility.FromJson<Message>(returnData);
        try
        {
            switch (latestMessage.cmd)
            {
                case commands.NEW_CLIENT:
                    Debug.Log("A new client has connected: " + IncomingMessage);
                    NewPlayer p = JsonUtility.FromJson<NewPlayer>(returnData);
                    if (myID == "newID") 
                    { 
                        myID = p.player.id; 
                    }
                    PlayersToSpawnList.Add(p.player.id);
                    break;

                case commands.UPDATE: 
                    latestGameState = JsonUtility.FromJson<GameState>(returnData);
                    break;

                case commands.CLIENT_REMOVED:
                    DroppedPlayer d = JsonUtility.FromJson<DroppedPlayer>(returnData);
                    Debug.Log("A client has disconnected: " + IncomingMessage);
                    PlayersToDestroyList.Add(d.id);//Destroy that player by its id.;
                    break;

                case commands.GET_PLAYERS_IN_GAME:
                    PlayersIG playersIG = JsonUtility.FromJson<PlayersIG>(returnData);
                    for (int i = 0; i < playersIG.players.Length; i++)
                    {
                        if (playersIG.players[i].id != myID)
                            PlayersToSpawnList.Add(playersIG.players[i].id);
                    }
                    break;

                default:
                    Debug.Log("Error");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }

        socket.BeginReceive(new AsyncCallback(OnReceived), socket);
    }


    void SpawnPlayersFromList()
    {
        for (int i = 0; i < PlayersToSpawnList.Count; i++)
        {
            GameObject o = Instantiate(PlayerPrefab, new Vector3(UnityEngine.Random.Range(-5.0f, 5.0f), UnityEngine.Random.Range(-5.0f, 5.0f), 0.0f), Quaternion.identity);
            o.GetComponent<PlayerCube>().ClientID = PlayersToSpawnList[i];
            PlayersInGameList.Add(o.GetComponent<PlayerCube>());
            o.GetComponent<PlayerCube>().r = UnityEngine.Random.Range(0.0f, 1.0f);
            o.GetComponent<PlayerCube>().g = UnityEngine.Random.Range(0.0f, 1.0f);
            o.GetComponent<PlayerCube>().b = UnityEngine.Random.Range(0.0f, 1.0f);
            //renderer.material.color = Color(0.5, 1, 1); //
        }

        PlayersToSpawnList.Clear();
        PlayersToSpawnList.TrimExcess();

    }


    void UpdatePlayers()
    {
        for (int i = 0; i < latestGameState.players.Length; i++)
        {
            if (latestGameState.players[i].id != myID)
            {
                for (int j = 0; j < PlayersInGameList.Count; j++)
                {
                    if (PlayersInGameList[j].ClientID == latestGameState.players[i].id)
                    {
                        float XX = latestGameState.players[i].position.x;
                        float YY = latestGameState.players[i].position.y;
                        float ZZ = latestGameState.players[i].position.z;
                        //PlayersInGameList[j].transform.position = new Vector3(XX, YY, ZZ);
                    }
                }
            }
        }
    }

    void DestroyPlayers()
    {
        for (int i = 0; i < PlayersToDestroyList.Count; i++)
        {
            for (int ii = 0; ii < PlayersInGameList.Count; ii++)
            {
                if (PlayersInGameList[ii].ClientID == PlayersToDestroyList[ii])
                {
                    PlayersInGameList[ii].gameObject.SendMessage("DestroyCube");
                    Debug.Log("Attempt to destroy object");
                }
            }
        }
        PlayersToDestroyList.Clear();
        PlayersToDestroyList.TrimExcess();
    }

    void HeartBeat()
    {
        Byte[] sendBytes = Encoding.ASCII.GetBytes("heartbeat");
        udp.Send(sendBytes, sendBytes.Length);
    }

    public void SendPlayerInfo(Vector3 position)
    {
        PlayerPosition playerVectors = new PlayerPosition();

        playerVectors.position = position;
        Byte[] sendBytes = Encoding.ASCII.GetBytes(JsonUtility.ToJson(playerVectors));
        udp.Send(sendBytes, sendBytes.Length);

    }

    void Update()
    {
        SpawnPlayersFromList();
        DestroyPlayers();
        UpdatePlayers();
    }
}
