using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Platformer2
{
    public class Viewer
    {
        public Vector2 position = new Vector2(100);
        private readonly float speed = 550f;

        public Viewer() { }

        public void Update()
        {
            var kbState = Keyboard.GetState();
            float deltatime = AllUsing.time;
            Vector2 velocity = Vector2.Zero;

            if (kbState.IsKeyDown(Keys.A))
            {
                velocity.X -= speed * deltatime;
            }
            if (kbState.IsKeyDown(Keys.D))
            {
                velocity.X += speed * deltatime;
            }
            if (kbState.IsKeyDown(Keys.W))
            {
                velocity.Y -= speed * deltatime;
            }
            if (kbState.IsKeyDown(Keys.S))
            {
                velocity.Y += speed * deltatime;
            }

            position += velocity;
            if (position.X < 0 + (AllUsing.screenWidth / 2))
                position.X = 0 + (AllUsing.screenWidth / 2);
            if (position.Y < 0 + (AllUsing.screenHeight / 2))
                position.Y = 0 + (AllUsing.screenHeight / 2);
            if (position.X > AllUsing.map.dimensions.X - (AllUsing.screenWidth / 2))
                position.X = AllUsing.map.dimensions.X - (AllUsing.screenWidth / 2);
            if (position.Y > AllUsing.map.dimensions.Y - (AllUsing.screenHeight / 2))
                position.Y = AllUsing.map.dimensions.Y - (AllUsing.screenHeight / 2);
        }
    }
}
