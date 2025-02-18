using System;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;


namespace Platformer2
{
    public delegate void closeGame();
    public delegate void ColorScheme(int mapNumber);
    class Client
    {
        protected Thread thread;
        protected TcpListener listener;
        protected TcpClient client;
        protected int port;
        protected BinaryWriter sender;
        protected BinaryReader reader;
        public bool IsConnected;
        public event closeGame exit;
        public event ColorScheme eventColorScheme;
        public bool renderedHostMap = false;
        public static readonly object updateLock = new object();

        public int id;

        public string name;
        public string password;

        int count = 0;
        string IP;
        public Client(string IP, int port)
        {
            this.IP = IP;
            this.port = port;
        }
        public void startCommunicating()
        { // opening a Thread for the communication of the client and the server
            thread = new Thread(communicateWithServer)
            {
                IsBackground = true
            };
            thread.Start();
        }

        private void communicateWithServer()
        { // the non stopping sending and recieving messages from and to the server
            try
            {
                client = new TcpClient();
                client.Connect(IP, port);

                reader = new BinaryReader(client.GetStream());
                sender = new BinaryWriter(client.GetStream());

                IsConnected = true;
                acceptHandshake();

                string receivedID = reader.ReadString();
                receivedID = Encryption.DecryptString(receivedID, symmetricKey);
                Protocol.acceptID(receivedID);
                this.id = AllUsing.myPlayer.id;

                string theme = reader.ReadString();
                theme = Encryption.DecryptString(theme, symmetricKey);
                int mapColor = Protocol.colorScheme(theme);
                eventColorScheme.Invoke(mapColor);
                renderedHostMap = true;

                while (this.name == null)
                {
                }
                Protocol.sendName(sender, symmetricKey, AllUsing.myPlayer);
                while (this.password == null)
                {
                }
                Protocol.sendPassword(sender, symmetricKey, password, id);
                Protocol.sendName(sender, symmetricKey, AllUsing.myPlayer);

                didHandshake = true;
                // Initialized all the basic information about the client, now starting the updating until the client/host disconnect

                Thread sendingThread = new Thread(sendPlayer)
                {
                    IsBackground = true
                };
                sendingThread.Start();

                receiveData();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Failed to connect" + ex);
                exit?.Invoke();
            }
        }
        public void sendPlayer()
        {
            while (true)
            {
                try
                {
                    Protocol.sendPlayerInfo(sender, symmetricKey, AllUsing.myPlayer);
                    Thread.Sleep(20);
                }
                catch
                {
                    disconnect();
                }
            }
        }
        public static readonly object exitLock = new object();
        public void disconnect()
        {
            lock (exitLock)
            {
                System.Windows.Forms.MessageBox.Show("Lost connection, disconnecting...");
                client.Close();
                sender?.Dispose();
                reader?.Dispose();
                exit?.Invoke();
            }
        }
        private void receiveData()
        {
            //receive the messages from the server
            string paket = " ";
            while (true)
            {
                try
                {
                    paket = reader.ReadString();
                    if (paket == "wrong user")
                    {
                        System.Windows.Forms.MessageBox.Show("wrong user");
                        exit();
                    }
                    try
                    {
                        paket = Encryption.DecryptString(paket, symmetricKey);
                        Protocol.ParseMessage(paket);
                    }
                    catch
                    {
                        System.Windows.Forms.MessageBox.Show("failed to decrypt: " + id + "      " +
                            " " + paket);
                    }
                    
                }
                catch
                {
                    disconnect();
                }
            }
        }

        RSAParameters serverPublicKey;
        byte[] symmetricKey;
        public bool didHandshake = false;
        public void acceptHandshake()
        { //Accepts the triple handshake. gets the public key, and
            // sends the client's symmetric key to the server
            //as a string encrypted by the public key
            string stringKey = reader.ReadString();
            serverPublicKey = Encryption.CastStringToRSAKey(stringKey);

            symmetricKey = Encryption.GenerateSymmetricKey();
            using (RSACryptoServiceProvider rsaEncrypt = new RSACryptoServiceProvider())
            {
                rsaEncrypt.ImportParameters(serverPublicKey);
                byte[] encryptedSymmetricKey = rsaEncrypt.Encrypt(symmetricKey, false);
                string encryptedSymmetricKeyBase64 = Convert.ToBase64String(encryptedSymmetricKey);

                sender.Write(encryptedSymmetricKeyBase64);
            }
        }
    }
}