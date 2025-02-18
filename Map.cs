using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Platformer2
{
    public class Map
    {
        //public Vector2 Scale;
        public Vector2 dimensions;
        private TileType[,] tiles;

        public TileType returnGround(Vector2 pos)
        {
            // returns which kind of ground the asked pixel is
            if ((pos.X) < 0 || (pos.X) >= tiles.GetLength(0) ||
                    pos.Y < 0 || pos.Y >= tiles.GetLength(1))
                return TileType.tile;
            return tiles[(int)(pos.X), (int)(pos.Y)];
        }

        public Map(Texture2D tex)
        { //Initialize the map, inserting the information to the 2D arrey
            tiles = new TileType[tex.Width, tex.Height];
            dimensions = new Vector2(tex.Width, tex.Height);
            Color[] texColor = new Color[tex.Width * tex.Height];
            tex.GetData<Color>(texColor);

            for (int x = 0; x < tex.Width; x++)
            {
                for (int y = 0; y < tex.Height; y++)
                {
                    if (texColor[x + y * tex.Width] == Color.Black)
                        tiles[x, y] = TileType.tile;
                    else
                        tiles[x, y] = TileType.air;

                }
            }
        }
    }
}
