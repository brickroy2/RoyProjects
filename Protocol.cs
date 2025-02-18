using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace Platformer2
{

    public class Protocol
    {
        public static void ParseMessage(string message)
        { // recieve a message from the other side of the connection (server/ client) and parse it
            try
            {
                string[] parts = message.Split(':');
                string command = parts[0];
                if (command == "wrong user")
                    return;
                int id = int.Parse(parts[1]);
                string data = parts[2];
                if (id == AllUsing.myPlayer.id)
                {
                    if (command == "scored")
                        scored(id);
                    return;
                }
                if (!AllUsing.players.Any(p => p.id == id))
                {
                    Player temp = new Player(new Vector2(100, 100));
                    temp.id = id;
                    AllUsing.players.Add(temp);
                }
                if (command == "position")
                {
                    UpdatePosition(id, data);
                }
                else if (command == "name")
                {
                    UpdateName(id, data);
                }
                else if (command == "shot")
                {
                    shot(id);
                }

                else if (command == "removePlayer")
                {
                    removePlayer(id);
                }
                else if (command == "acceptID")
                {
                    acceptID(message);
                }
                else if (command == "scored")
                {
                    scored(id);
                }
                else if (command == "winner")
                {
                    winnerPlayer(id);
                }
                else if (command == "hit")
                {
                    hit(id);
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("Unknown command: " + command + " message: " + message);
                }
            }
            catch
            {
                System.Windows.Forms.MessageBox.Show("failed to parse the message " + message);
            }

        }

        public static void scored(int id)
        { //adding a point to the player who killed another player
            if (id == AllUsing.myPlayer.id)
            {
                AllUsing.myPlayer.score++;
                return;
            }
            Player player = AllUsing.players.Find(p => p.id == id);
            if (player != null)
            {
                player.score++;
            }
        }
        public static void hit(int id)
        { //removing bullet that hit his target
            if (id == AllUsing.myPlayer.id)
            {
                AllUsing.myPlayer.bulletsToRemove++;
                return;
            }
            Player player = AllUsing.players.Find(p => p.id == id);
            if (player != null)
            {
                player.bulletsToRemove++;
            }
        }

        public static void removePlayer(int id)
        { // a player to remove because he disconnected
            lock (AllUsing.players)
            {
                Player toRemove = AllUsing.players.Find(p => p.id == id);
                if (toRemove != null)
                {
                    AllUsing.players.Remove(toRemove);
                }
            }
        }
        private static void UpdatePosition(int id, string data)
        { // updates the player position, directions and lives 
            string[] coords = data.Split(',');
            if (float.TryParse(coords[0], out float x) && float.TryParse(coords[1], out float y))
            {
                Player player = AllUsing.players.Find(p => p.id == id);
                if (player != null)
                {
                    player.Position = new Vector2(x, y);
                    if (coords[2] == "Left")
                        player.direction = Direction.Left;
                    else
                        player.direction = Direction.Right;
                    if (int.Parse(coords[3]) > 0)
                    {
                        player.isAlive = true;
                        player.hearts = int.Parse(coords[3]);
                    }

                    else
                    {
                        player.isAlive = false;
                        player.hearts = 0;
                    }
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("Adding Player. id:" + id);
                    Player temp = new Player(new Vector2(100, 100));
                    temp.id = id;
                    AllUsing.players.Add(temp);
                }
            }
            else
            {
                Console.WriteLine("Invalid position values");
            }
        }
        private static void shot(int id)
        {
            Player player = AllUsing.players.Find(p => p.id == id);
            player.shoot(player.direction);
        }
        private static void UpdateName(int id, string data)
        { //Updates the player's name
            Player player = AllUsing.players.Find(p => p.id == id);
            if (player != null)
            {
                player.name = data;
            }
        }

        public static string returnPassword(string message)
        {
            string[] parts = message.Split(':');
            return parts[2];
        }
        public static string returnData(string message)
        {
            string[] parts = message.Split(':');
            return parts[2];
        }
        public static void acceptID(string message)
        { // Gets the client ID from the server for communication
            string[] parts = message.Split(':');
            int id = int.Parse(parts[1]);
            AllUsing.myPlayer.id = id;

        }
        public static int colorScheme(string message)
        {
            string[] parts = message.Split(':');
            string scheme = parts[1];
            if (scheme == "dark")
                return 2;
            if (scheme == "dry")
                return 3;
            return 1;
        }
        public static void winnerPlayer(int id)
        {
            Player player = AllUsing.players.Find(p => p.id == id);
            player.won = true;
            AllUsing.winningPlayerID = id;
        }

        public static void sendPlayerInfo(BinaryWriter sender, byte[] symmetricKey, Player player)
        { //sends all the information about the player
            string toSend = makePositionString(player);
            sender.Write(Encryption.EncryptString(toSend, symmetricKey));
            Thread.Sleep(5);
            if (player.hasShot)
            {
                toSend = "shot" + ":" + player.id + ":  ";
                Thread.Sleep(5);
                sender.Write(Encryption.EncryptString(toSend, symmetricKey));
                player.hasShot = false;
            }
            int scoredID = player.killerID;
            if (!(scoredID == -1))
            {
                toSend = "scored:" + scoredID + ": ";
                Thread.Sleep(5);
                sender.Write(Encryption.EncryptString(toSend, symmetricKey));
                player.killerID = -1;
            }
            if (!(player.shooterID == -1))
            {
                toSend = "hit:" + player.shooterID + ": ";
                Thread.Sleep(5);
                sender.Write(Encryption.EncryptString(toSend, symmetricKey));
                player.shooterID = -1;
            }
            if (AllUsing.myPlayer.won)
            {
                toSend = "winner:" + AllUsing.myPlayer.id + ": ";
                Thread.Sleep(5);
                sender.Write(Encryption.EncryptString(toSend, symmetricKey));
            }
        }
        public static void sendName(BinaryWriter sender, byte[] symmetricKey, Player player)
        { //Send the player's name in the start of the game
            string toSend = "name:" + player.id + ":" + player.name;
            sender.Write(Encryption.EncryptString(toSend, symmetricKey));
        }
        public static void sendPassword(BinaryWriter sender, byte[] symmetricKey, string password, int id)
        { //Send the player's password as a hash in the start of the game
            string toSend = "password:" + id + ":" + password;
            sender.Write(Encryption.EncryptString(toSend, symmetricKey));
        }

        public static string makePositionString(Player player)
        { //make the position of the player a string for the protocol
            string send = "position:" + player.id + ":" + player.Position.X + "," + player.Position.Y + ",";
            if (player.direction == Direction.Left)
                send += "Left,";
            else
                send += "Right,";

            send += player.hearts;
            return send;
        }

        public static string makeDisconnectingMessage(int id)
        {
            return "removePlayer:" + id + ": ";
        }




    }
}