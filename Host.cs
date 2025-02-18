using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;


namespace Platformer2
{

    class Host
    {
        protected TcpListener listener;
        protected TcpClient client;
        protected int port;
        public bool IsConnected;

        public static List<User> Users { get; private set; }

        int nextID = 0;

        public Host(int port)
        { //Creates the host
            this.port = port;
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            AllUsing.myPlayer.id = 0;
            AllUsing.myPlayer.name = "Host";
            LoadUsers();
            AllUsing.writeLog("Hosting the server");
        }
        public static readonly object clientsLock = new object();
        private static readonly ReaderWriterLockSlim slimLock = new ReaderWriterLockSlim();
        public void AcceptClients()
        { // Waiting for new clients and adding them to the game
            while (true)
            {
                try
                {
                    TcpClient tcpClient = listener.AcceptTcpClient();
                    AllUsing.writeLog("Accepts a new client");
                    nextID++;
                    ClientAtServer clientAtServer = new ClientAtServer(nextID, tcpClient);
                    slimLock.EnterWriteLock();
                    try
                    {
                        AllUsing.clients.Add(clientAtServer);
                    }
                    finally
                    {
                        slimLock.ExitWriteLock();
                    }
                    Thread clientThread = new Thread(clientAtServer.runClient)
                    {
                        IsBackground = true
                    };
                    clientAtServer.eventDisconnectClient += clientDisconnected;
                    clientAtServer.eventConfirmedPassword += confirmedPassword;
                    clientAtServer.eventPassMessage += passToAll;
                    clientThread.Start();
                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error accepting client: " + ex.Message);
                    break;
                }
            }
        }

        public static void passToAll(string message, int senderID)
        { //Passing the wanted message to all the other clients
            AllUsing.clients.TrimExcess();
            slimLock.EnterReadLock();
            foreach (var client in AllUsing.clients)
            {
                if (client != null && client.clientID != senderID && client.didHandshake && client.running)
                {
                    AllUsing.writeLog("passing message: " + message);
                    client.sendData(message);
                }
            }
            slimLock.ExitReadLock();
        }
        private void confirmedPassword(ClientAtServer client, User user)
        { //checks if the user exists. Disconnects him otherwise
            AllUsing.writeLog("confirms the user");
            if (ValidateUser(user))
            {
                AllUsing.writeLog("User exists");
                client.Update();
            }
            else
            {
                AllUsing.writeLog("User doesn't exist");
                client.sendData("wrong user");
                clientDisconnected(client);
            }
        }
        public static readonly object lock1 = new object();
        private void clientDisconnected(ClientAtServer client)
        { //Removes the client that disconnected and send the message to all the connected clients 
            lock (lock1)
            {
                AllUsing.writeLog("Entered clientDisconnected from the host");
                client.running = false;
                AllUsing.writeLog("removed without using the locks");
                string message = Protocol.makeDisconnectingMessage(client.clientID);
                Protocol.removePlayer(client.clientID);
                foreach (var client1 in AllUsing.clients)
                {
                    if (client1 != null && client1.clientID != client.clientID && client1.didHandshake && client1.running)
                    {
                        AllUsing.writeLog("sending disconnecting message: " + message);
                        client1.sendData(message);
                    }
                } 
            }
        }
        public static void LoadUsers()
        { //Loads all the users from the json file
            AllUsing.writeLog("Loading users");
            string jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HashFile.json");
            if (File.Exists(jsonFilePath))
            {
                string json = File.ReadAllText(jsonFilePath);
                Users = JsonSerializer.Deserialize<List<User>>(json);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("couldn't load users");
                Users = new List<User>();
            }
        }
        private bool ValidateUser(User other)
        { //checks if user exists. returns false if not
            foreach (var user in Users)
            {
                if (user.isEqual(other))
                {
                    return true;
                }
            }
            return false;
        }


    }
}