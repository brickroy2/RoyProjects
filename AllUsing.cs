using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;

namespace Platformer2
{
    public enum TileType
    {
        tile,
        air
    }
    public class User
    {
        public string name { get; set; }
        public string password { get; set; }

        public User() { }
        public User(string name, string password)
        {
            this.name = name;
            this.password = password;
        }
        public bool isEqual(User other)
        {
            if (this.name == other.name && this.password == other.password)
                return true;
            return false;

        }
    }

    public static class AllUsing
    {
        public static float time;
        public static GameTime gameTime;
        public static ContentManager content;
        public static SpriteBatch spriteBatch;
        public static GraphicsDevice graphicsDevice;
        public static int screenWidth;
        public static int screenHeight;
        public static SpriteFont font;
        public static Map map;
        public static Color fontColor;
        public static Player myPlayer;
        public static List<Player> players = new List<Player>();
        public static List<ClientAtServer> clients = new List<ClientAtServer>();
        public static bool isServer;
        public static Vector2 topLeft;
        public static int winningPlayerID;

        public static void Update(GameTime gt)
        {
            time = (float)gt.ElapsedGameTime.TotalSeconds;
        }
        private static readonly string logFilePath = "log.txt";
        public static readonly object lock1 = new object();
        public static void writeLog(string message)
        {
            lock (lock1)
            {
                try
                {
                    using (StreamWriter sw = new StreamWriter(logFilePath, true))
                    {
                        sw.WriteLine(message);
                    }
                }
                catch
                {
                    System.Windows.Forms.MessageBox.Show("couldn't find log");
                    using (StreamWriter sw = new StreamWriter("log.txt", false))
                    {
                        sw.WriteLine("Starting logger");
                        sw.WriteLine(message);
                    }
                }
            }
        }
    }
}
