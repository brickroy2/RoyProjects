using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace Platformer2
{
    enum GameStatus
    {
        Starting,
        EnteringName,
        EnteringPassword,
        Connecting,
        Initializing,
        Playing,
        Finished
    }

    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        Texture2D backgroundMono;
        Texture2D colorBackground;
        Texture2D smallerMap;

        private Camera camera;
        private Viewer viewer;

        Host server;
        Client client;
        GameStatus status = GameStatus.Starting;
        private KeyboardState previousKbState;

        User serverUser;
        string username;
        string password;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            AllUsing.content = Content;
            AllUsing.spriteBatch = new SpriteBatch(GraphicsDevice);
            AllUsing.graphicsDevice = GraphicsDevice;
            AllUsing.screenWidth = GraphicsDevice.Viewport.Width;
            AllUsing.screenHeight = GraphicsDevice.Viewport.Height;

            AllUsing.myPlayer = new Player();
            AllUsing.myPlayer.id = -1;

            base.Initialize();
        }
        protected override void LoadContent()
        {
            AllUsing.spriteBatch = new SpriteBatch(GraphicsDevice);
            AllUsing.graphicsDevice = GraphicsDevice;
            camera = new Camera();

            Random random = new Random();
            int randomNumber = random.Next(1, 4);
            LoadMapContent(randomNumber);
            AllUsing.font = Content.Load<SpriteFont>("ArialFont");

            string jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "JSON_pass.json");
            if (File.Exists(jsonFilePath))
            {
                jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "JSON_pass.json");
                string json = File.ReadAllText(jsonFilePath);
                serverUser = JsonSerializer.Deserialize<User>(json);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("JSON file not found.");
                Exit();
            }
        }
        public void exitGame() => Exit();

        public void LoadMapContent(int mapNumber)
        {
            if (mapNumber == 1)
            {
                backgroundMono = AllUsing.content.Load<Texture2D>("bigBackgroundLightMono");
                colorBackground = AllUsing.content.Load<Texture2D>("bigBackgroundLight");
                smallerMap = AllUsing.content.Load<Texture2D>("bigBackgroundLightSmall");
                AllUsing.fontColor = Color.Black;
            }
            else if (mapNumber == 2)
            {
                backgroundMono = AllUsing.content.Load<Texture2D>("DarkerMapMono");
                colorBackground = AllUsing.content.Load<Texture2D>("DarkerMap");
                smallerMap = AllUsing.content.Load<Texture2D>("DarkerMapSmall");
                AllUsing.fontColor = Color.White;
            }
            else if (mapNumber == 3)
            {
                backgroundMono = AllUsing.content.Load<Texture2D>("dryMapMono");
                colorBackground = AllUsing.content.Load<Texture2D>("dryMap");
                smallerMap = AllUsing.content.Load<Texture2D>("dryMapSmaller");
                AllUsing.fontColor = Color.Brown;
            }
            AllUsing.map = new Map(backgroundMono);
        }


        protected override void Update(GameTime gameTime)
        {
            AllUsing.gameTime = gameTime;
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == Microsoft.Xna.Framework.Input.ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape))
                Exit();

            if (client != null)
            {
                client.exit += Exit;
                client.eventColorScheme += LoadMapContent;
            }

            AllUsing.Update(gameTime);
            var kbState = Keyboard.GetState();
            switch (status)
            {
                case GameStatus.Starting:
                    status = GameStatus.EnteringName;
                    break;
                case GameStatus.EnteringName:
                    HandleNameInput(kbState);
                    break;
                case GameStatus.EnteringPassword:
                    HandleNameInput(kbState);
                    break;
                case GameStatus.Connecting:
                    ConnectByInput();
                    break;
                case GameStatus.Initializing:
                    if ((bool)(client?.renderedHostMap))
                        status = GameStatus.Playing;
                    break;
                case GameStatus.Playing:
                    updateGame();
                    break;
                case GameStatus.Finished:
                    break;
            }
            if (server == null)
                this.Window.Title = status.ToString();
            else
                this.Window.Title = "Host";

            base.Update(gameTime);
        }

        private void ConnectByInput()
        { //connects to either client or host by the username and password
            password = Encryption.Hash(password);
            User connectingUser = new User(username, password);
            if (server == null && client == null)
            {
                if (connectingUser.isEqual(serverUser))
                {
                    try
                    {
                        server = new Host(6240);
                        Thread acceptThread = new Thread(server.AcceptClients);
                        acceptThread.IsBackground = true;
                        acceptThread.Start();
                        System.Windows.Forms.MessageBox.Show("Hosting the game.");
                        AllUsing.isServer = true;
                        viewer = new Viewer();
                        status = GameStatus.Playing;
                        AllUsing.writeLog("Starting");
                    }
                    catch (Exception ex)
                    {
                        System.Windows.Forms.MessageBox.Show("Exception occurred: " + ex.Message);
                        Exit();
                    }
                }
                else
                {
                    try
                    {
                        client = new Client("127.0.0.1", 6240);
                        client.startCommunicating();
                        client.name = username;
                        AllUsing.myPlayer.name = username;
                        client.password = password;
                        status = GameStatus.Initializing;
                    }
                    catch (Exception ex)
                    {
                        System.Windows.Forms.MessageBox.Show("Exception occurred: " + ex.Message);
                        Exit();
                    }
                }
            }
        }
        private void updateGame()
        { //updating the game for the status of playing
            if (!AllUsing.isServer)
            {
                AllUsing.myPlayer.Update();
                if (AllUsing.myPlayer.won)
                {
                    AllUsing.winningPlayerID = AllUsing.myPlayer.id;
                    status = GameStatus.Finished;
                }
            }
            foreach (var player in AllUsing.players)
            {
                player?.moveBullets();
                if (player.won)
                {
                    status = GameStatus.Finished;
                }
            }
            if (AllUsing.isServer && viewer != null)
            {
                viewer.Update();
                camera.MoveToward(viewer.position);
            }
            else
            {
                camera.MoveToward(AllUsing.myPlayer.Position);
            }
            AllUsing.topLeft = camera.GetTopLeft();
        }


        string input = "";
        private void HandleNameInput(KeyboardState kbState)
        {
            bool shiftPressed = kbState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || kbState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift);
            foreach (var key in kbState.GetPressedKeys())
            {
                if (previousKbState.IsKeyUp(key))
                {
                    if (key == Microsoft.Xna.Framework.Input.Keys.Enter)
                    {
                        if (status == GameStatus.EnteringName)
                        {
                            username = input;
                            status = GameStatus.EnteringPassword;
                            input = "";
                        }
                        else
                        {
                            password = input;
                            status = GameStatus.Connecting;
                        }

                    }
                    else if (key == Microsoft.Xna.Framework.Input.Keys.Back && input.Length > 0)
                    {
                        input = input.Substring(0, input.Length - 1);
                    }
                    else if (key == Microsoft.Xna.Framework.Input.Keys.Space)
                    {
                        input += "";
                    }
                    else if (key >= Microsoft.Xna.Framework.Input.Keys.A && key <= Microsoft.Xna.Framework.Input.Keys.Z) // Ensuring only letter keys are processed
                    {
                        char letter = (char)(key - Microsoft.Xna.Framework.Input.Keys.A + (shiftPressed ? 'A' : 'a'));
                        input += letter;
                    }
                    else if (key >= Microsoft.Xna.Framework.Input.Keys.D0 && key <= Microsoft.Xna.Framework.Input.Keys.D9)
                    {
                        char number = (char)(key - Microsoft.Xna.Framework.Input.Keys.D0 + '0');
                        input += number;
                    }
                }
            }

            previousKbState = kbState;
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            if (status == GameStatus.EnteringPassword || status == GameStatus.EnteringName)
            {
                AllUsing.spriteBatch.Begin();
                AllUsing.spriteBatch.Draw(smallerMap, new Vector2(0), Color.White);
                if (status == GameStatus.EnteringName)
                {
                    string promptText = "Enter name";
                    Vector2 promptSize = AllUsing.font.MeasureString(promptText);
                    Vector2 promptPosition = new Vector2((AllUsing.screenWidth - promptSize.X) / 2, 36);
                    AllUsing.spriteBatch.DrawString(AllUsing.font, promptText, promptPosition, AllUsing.fontColor);
                    string nameText = input;
                    Vector2 nameSize = AllUsing.font.MeasureString(nameText);
                    Vector2 namePosition = new Vector2((AllUsing.screenWidth - nameSize.X) / 2, 64);
                    AllUsing.spriteBatch.DrawString(AllUsing.font, nameText, namePosition, AllUsing.fontColor);
                }
                if (status == GameStatus.EnteringPassword)
                {
                    string promptText = "Enter Password";
                    Vector2 promptSize = AllUsing.font.MeasureString(promptText);
                    Vector2 promptPosition = new Vector2((AllUsing.screenWidth - promptSize.X) / 2, 36);
                    AllUsing.spriteBatch.DrawString(AllUsing.font, promptText, promptPosition, AllUsing.fontColor);
                    string hidePass = " ";
                    for (int i = 0; i <input.Length-1; i++)
                    {
                        hidePass += "*";
                    }
                    if (input.Length >0)
                        hidePass += input.Substring(input.Length - 1);
                    string nameText = hidePass;
                    Vector2 nameSize = AllUsing.font.MeasureString(nameText);
                    Vector2 namePosition = new Vector2((AllUsing.screenWidth - nameSize.X) / 2, 64);
                    AllUsing.spriteBatch.DrawString(AllUsing.font, nameText, namePosition, AllUsing.fontColor);
                }
                AllUsing.spriteBatch.End();
                return;
            }
            if (status == GameStatus.Initializing)
            {
                AllUsing.spriteBatch.Begin();
                AllUsing.spriteBatch.Draw(smallerMap, new Vector2(0), Color.White);
                string promptText = "Initializing the game...";
                Vector2 promptSize = AllUsing.font.MeasureString(promptText);
                Vector2 promptPosition = new Vector2((AllUsing.screenWidth - promptSize.X) / 2, (AllUsing.screenHeight / 2));
                AllUsing.spriteBatch.DrawString(AllUsing.font, promptText, promptPosition, AllUsing.fontColor);
                AllUsing.spriteBatch.End();
                return;
            }


            Matrix transform = Matrix.CreateTranslation(-camera.GetTopLeft().X, -camera.GetTopLeft().Y, 0);
            AllUsing.spriteBatch.Begin(SpriteSortMode.Immediate, null, null, null, null, null, transform);

            AllUsing.spriteBatch.Draw(colorBackground, new Vector2(0, 0), Color.White);

            if (server == null)
                AllUsing.myPlayer.Draw(Color.White);

            foreach (Player player in AllUsing.players)
            {
                if (player != null)
                {
                    player.Draw(Color.DarkGray);
                }
            }
            if (status == GameStatus.Finished)
            {
                if (AllUsing.myPlayer.id == AllUsing.winningPlayerID)
                {
                    AllUsing.spriteBatch.DrawString(AllUsing.font, "Finished game, " + AllUsing.myPlayer.name + " won the game", new Vector2(AllUsing.screenWidth / 2, AllUsing.screenHeight / 2), AllUsing.fontColor);
                }
                else
                {
                    Player player = AllUsing.players.Find(p => p.id == AllUsing.winningPlayerID);
                    string finalMessage = "Finished game, " + player.name + " won the game";
                    Vector2 messageSize = AllUsing.font.MeasureString(finalMessage);
                    Vector2 messagePos = new Vector2(camera.GetTopLeft().X + ((AllUsing.screenWidth - messageSize.X) / 2), camera.GetTopLeft().Y + ((AllUsing.screenHeight / 2) - 100));
                    AllUsing.spriteBatch.DrawString(AllUsing.font, finalMessage, messagePos, AllUsing.fontColor);
                }
            }
            AllUsing.spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}

/*
 * Name: Host
 * Password: HostPassword
 * 
 * Name: Luke
 * Password: password
 * 
 * Name: Slayer1
 * Password: 1234
 * 
 * Name: Roy
 * Password: project
 * 
 * Name: Scout
 * Password: darth
*/