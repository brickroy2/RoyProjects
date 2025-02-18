using Microsoft.Xna.Framework;

namespace Platformer2
{
    internal class Camera
    {
        public Vector2 Center { get; set; }
        private Vector2 quarterScreen;

        public Vector2 GetTopLeft()
        { //Getting the top left edge of the screen.
            //prevents the camera from going out of the map
            Vector2 cameraPos = Center - quarterScreen;
            if (cameraPos.X < 0 )
                cameraPos.X = 0;
            if ( cameraPos.Y < 0 )
                cameraPos.Y = 0;
            if (cameraPos.X > AllUsing.map.dimensions.X - AllUsing.screenWidth)
                cameraPos.X = AllUsing.map.dimensions.X - AllUsing.screenWidth;
            if (cameraPos.Y > AllUsing.map.dimensions.Y - AllUsing.screenHeight)
                cameraPos.Y = AllUsing.map.dimensions.Y - AllUsing.screenHeight;
            return cameraPos;
        }

        public Camera()
        {
            quarterScreen = new Vector2(AllUsing.screenWidth / 2, AllUsing.screenHeight / 2);
        }
        public void MoveToward(Vector2 target, float movePercentage = .04f)
        { // "Follows" the player by the percentage wanted
            Vector2 delta = target - Center; 
            delta *= movePercentage;
            Center += delta;

            if ((target - Center).Length() < movePercentage)
            {
                Center = target;
            }
        }
    }
}
