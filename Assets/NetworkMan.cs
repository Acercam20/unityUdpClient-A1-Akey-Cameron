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
    public string myID;
    public GameObject prefab;
    public List<string> SpawnList;
    public List<string> DestroyList;
    public List<PlayerCube> InGameList;
    //public float mX = 1, mY = 1, mZ = 1;

    void Start()
    {
        udp = new UdpClient();
        udp.Connect("3.19.54.254", 12345);
        Byte[] sendBytes = Encoding.ASCII.GetBytes("connect");
        udp.Send(sendBytes, sendBytes.Length);
        udp.BeginReceive(new AsyncCallback(OnReceived), udp);
        InvokeRepeating("HeartBeat", 1, 1);
    }

    void OnDestroy()
    {
        udp.Dispose();
    }

    [Serializable]
    public class Message
    {
        public commands cmd;
    }

    [Serializable]
    public class PlayerPosition { public Vector3 position; }

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
    public class NewPlayer { public Player player; }

    [Serializable]
    public class DroppedPlayer { public string id; }

    [Serializable]
    public class PlayersAlreadyInGame { public Player[] players; }

    [Serializable]
    public class GameState { public Player[] players; }

    public enum commands
    {
        NEW_PLAYER,     //0
        UPDATE,         //1
        PLAYER_REMOVED, //2
        ADD_PLAYERS,    //3
    };

    public Message latestMessage; public GameState latestGameState;

    void OnReceived(IAsyncResult result)
    {
        UdpClient socket = result.AsyncState as UdpClient;
        IPEndPoint source = new IPEndPoint(0, 0);
        byte[] message = socket.EndReceive(result, ref source);
        string returnData = Encoding.ASCII.GetString(message);
        IncomingMessage = "Got this" + returnData;

        latestMessage = JsonUtility.FromJson<Message>(returnData);

        try
        {
            switch (latestMessage.cmd)
            {
                case commands.NEW_PLAYER:
                    Debug.Log("New Player Arrived");
                    Debug.Log(IncomingMessage);
                    NewPlayer p = JsonUtility.FromJson<NewPlayer>(returnData);
                    if (myID == null) 
                    { 
                        myID = p.player.id; 
                    }
                    SpawnList.Add(p.player.id);
                    break;

                case commands.UPDATE:
                    latestGameState = JsonUtility.FromJson<GameState>(returnData);
                    break;

                case commands.PLAYER_REMOVED:
                    DroppedPlayer d = JsonUtility.FromJson<DroppedPlayer>(returnData);
                    Debug.Log("Player Left The Game:");
                    Debug.Log(IncomingMessage);
                    DestroyList.Add(d.id);
                    break;

                case commands.ADD_PLAYERS:
                    PlayersAlreadyInGame pigl = JsonUtility.FromJson<PlayersAlreadyInGame>(returnData);
                    for (int i = 0; i < pigl.players.Length; i++)
                    {
                        if (pigl.players[i].id != myID)
                            SpawnList.Add(pigl.players[i].id);
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

    void SpawnPlayers()
    {
        for (int i = 0; i < SpawnList.Count; i++)
        {
            GameObject o = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            o.GetComponent<PlayerCube>().ClientID = SpawnList[i];

            for (int j = 0; j < latestGameState.players.Length; j++)
            {
                if (o.GetComponent<PlayerCube>().ClientID == latestGameState.players[j].id)
                {
                    o.GetComponent<PlayerCube>().r = latestGameState.players[j].color.R;
                    o.GetComponent<PlayerCube>().g = latestGameState.players[j].color.G;
                    o.GetComponent<PlayerCube>().b = latestGameState.players[j].color.B;

                }
            }
            InGameList.Add(o.GetComponent<PlayerCube>());
        }
        SpawnList.Clear();
    }

    void UpdatePlayers()
    {
        for (int i = 0; i < latestGameState.players.Length; i++)
        {
            if (latestGameState.players[i].id != myID)
            {
                for (int j = 0; j < InGameList.Count; j++)
                {
                    if (InGameList[j].ClientID == latestGameState.players[i].id)
                    {
                        Debug.Log(latestGameState.players[i].color.R + ", " + latestGameState.players[i].color.G + ", " + latestGameState.players[i].color.B);
                    }
                }
            }
        }
    }

    void DestroyPlayers()
    {
        for (int j = 0; j < DestroyList.Count; j++)
        {
            for (int i = 0; i < InGameList.Count; i++)
            {
                if (InGameList[i].ClientID == DestroyList[j])
                {
                    InGameList[i].gameObject.SendMessage("DestroyCube");
                    Debug.Log("Attempt to destroy object");
                }
            }
        }
        DestroyList.Clear();
    }

    public void SendPlayerInfo(Vector3 position)
    {
        PlayerPosition playerVectors = new PlayerPosition();
        playerVectors.position = position;
        Byte[] sendBytes = Encoding.ASCII.GetBytes(JsonUtility.ToJson(playerVectors));
        udp.Send(sendBytes, sendBytes.Length);
    }

    void HeartBeat()
    {
        Byte[] sendBytes = Encoding.ASCII.GetBytes("heartbeat");
        udp.Send(sendBytes, sendBytes.Length);
    }

    void Update()
    {
        SpawnPlayers();
        UpdatePlayers();
        DestroyPlayers();
    }
}
