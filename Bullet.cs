using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Platformer2
{
    public enum Direction
    {
        Right,
        Left,
        Up
    }

    public class Bullet
    {
        public Texture2D texture;
        public Vector2 position; 
        public Direction direction; 
        private const float speed = 750f;

        Rectangle bounds;
        public Bullet(Vector2 position, Direction direction)
        {
            this.position = position;
            this.direction = direction;
            LoadContent(AllUsing.content);
        }

        public void LoadContent(ContentManager content)
        {
            texture = content.Load<Texture2D>("bullet");
            bounds = new Rectangle((int)position.X, (int)position.Y, texture.Width, texture.Height); 
        }
        public bool canGo(Direction direction)
        { // checks if the bullet can continue in his way or is he collapsing into a wall
            
            Rectangle futureBounds;
            if (direction == Direction.Left)
            {
                futureBounds = new Rectangle((int)(position.X - speed * AllUsing.time), (int)position.Y, texture.Width, texture.Height);
            }
            else if (direction == Direction.Right)
            {
                futureBounds = new Rectangle((int)(position.X + speed * AllUsing.time), (int)position.Y, texture.Width, texture.Height);
            }
            else
            {
                return false;
            }
            for (int x = futureBounds.Left; x < futureBounds.Right; x++)
            {
                for (int y = futureBounds.Top; y < futureBounds.Bottom; y++)
                {
                    Vector2 mapPos = new Vector2(x, y);
                    if (AllUsing.map.returnGround(mapPos) != TileType.air)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public bool Move()
        {
             // Move the bullet based on its direction
             switch (direction)
             {
                 case Direction.Right:
                     position.X += speed * AllUsing.time; 
                     break;
                 case Direction.Left:
                     position.X -= speed * AllUsing.time;
                     break;
                 case Direction.Up:
                     position.Y -= speed * AllUsing.time;
                     break;        
             }
             return true;
        }

        public void Draw()
        { //Drawing the bullet according to his direction
            if (direction == Direction.Left)
            {
                AllUsing.spriteBatch.Draw(texture, position, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.FlipHorizontally, 0f);
            }
            else
            {
                AllUsing.spriteBatch.Draw(texture, position, Color.White);
            }
        } 

    }
}
