using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace Platformer2
{
    public delegate void DisconnectClient(ClientAtServer client);
    public delegate void confirmedPassword(ClientAtServer client, User user);
    public delegate void passMessage(string message, int senderID);
    public class ClientAtServer
    {
        public int clientID;
        public bool running;
        public BinaryWriter sender;
        protected BinaryReader reader;
        public TcpClient client;
        protected int port;
        public event DisconnectClient eventDisconnectClient;
        public event confirmedPassword eventConfirmedPassword;
        public event passMessage eventPassMessage;
        public bool didHandshake = false;
        public string name { get; private set; }

        public ClientAtServer(int id, TcpClient client)
        {
            this.clientID = id;
            this.client = client;
        }
        public void runClient()
        { // Starting the communication of the client and the server.
          // when the user was confirmed the updates starts from the event 
            running = true;
            reader = new BinaryReader(client.GetStream());
            sender = new BinaryWriter(client.GetStream());
            threeWayHandshake();
            running = true;

            string idToSend = "acceptID:" + clientID + ": ";
            sender.Write(Encryption.EncryptString(idToSend, symmetricKey));
            AllUsing.writeLog("sent ID to client");

            string theme = "Map:dark: ";
            if (AllUsing.fontColor == Color.Black)
                theme = "Map:light: ";
            else if (AllUsing.fontColor == Color.Brown)
                theme = "Map:dry: ";
            sender.Write(Encryption.EncryptString(theme, symmetricKey));

            string message = reader.ReadString(); //name
            message = Encryption.DecryptString(message, symmetricKey);
            this.name = Protocol.returnData(message);
            Protocol.ParseMessage(message);

            message = reader.ReadString(); //hashed password
            message = Encryption.DecryptString(message, symmetricKey);
            string password = Protocol.returnPassword(message);
            eventConfirmedPassword?.Invoke(this, new User(name, password));

            // Initialized all the basic information about the client, now starting the constant update
        }
        public void Update()
        {
            this.didHandshake = true;
            foreach (var player in AllUsing.players)
            {
                if (player != null && player.id != this.clientID)
                    Protocol.sendName(sender, symmetricKey, player);
            }
            while (running)
            {
                try
                {
                    string message = reader.ReadString();
                    AllUsing.writeLog("recieved encrypted message: " +  message);
                    message = Encryption.DecryptString(message, symmetricKey);
                    AllUsing.writeLog("decrypted message: " + message);
                    eventPassMessage?.Invoke(message, clientID);
                    Protocol.ParseMessage(message);
                }
                catch
                {
                    AllUsing.writeLog("Communication failed. disconnecting client");
                    disconnect();
                    break;
                }
            }
        }
        public void disconnect()
        {
            running = false;
            sender?.Dispose();
            reader?.Dispose();
            client.Close();
            AllUsing.writeLog("invoking disconnecting event");
            eventDisconnectClient?.Invoke(this);
        }

        public static readonly object lock1 = new object();
        public void sendData(string message)
        { //Sending the wanted message to the client. Used to pass messages from one client to the others
            if (!this.didHandshake)
                return;
            lock (lock1)
            {
                try
                {
                    string toSend = Encryption.EncryptString(message, this.symmetricKey);
                    sender.Write(toSend);
                }
                catch
                {
                    disconnect();
                }
            }

        }

        RSAParameters publicKey, privateKey;
        public byte[] symmetricKey;
        public void threeWayHandshake()
        { // Doing the triple handshake. Creating an RSA object, sending the public key to
          // the client and recieving his symmetric key
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                publicKey = rsa.ExportParameters(false);
                privateKey = rsa.ExportParameters(true);
            }
            string publicKeyAsString = Encryption.CastRSAKeyToString(publicKey);
            sender.Write(publicKeyAsString);

            string encryptedSymmetricKeyBase64 = reader.ReadString();
            byte[] encryptedSymmetricKey = Convert.FromBase64String(encryptedSymmetricKeyBase64);

            using (RSACryptoServiceProvider rsaDecrypt = new RSACryptoServiceProvider())
            {
                rsaDecrypt.ImportParameters(privateKey);
                symmetricKey = rsaDecrypt.Decrypt(encryptedSymmetricKey, false);
            }
            AllUsing.writeLog("finished handshake");
        }

    }
}