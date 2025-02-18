using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Platformer2
{
    public delegate void OnShot();

    public class Player
    {
        public Texture2D Texture;
        public Texture2D heartTexture;
        public Texture2D emptyHeartTexture;
        public Vector2 Position;
        public bool isAlive = true;
        public int score = 0;
        private const float speed = 550f;
        private Vector2 _velocity;
        public Direction direction;
        float airTime = 0f;
        const float baseGravity = 500f;
        const float maxGravity = 10000f;
        const float maxAirTime = 500000f;
        float gravity = baseGravity;
        bool isOnGround = true;
        public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, Texture.Width, Texture.Height);
        public Queue<Bullet> bullets = new Queue<Bullet>();
        public string name;
        public int id;
        public int hearts;


        private bool isJumping;
        private Vector2 initialJumpPosition;
        private const float jumpDuration = 1.0f;
        private const float maxJumpHeight = 200.0f;
        private float jumpTimeElapsed;
        private const float initialJumpSpeed = 600f;
        private float currentJumpSpeed;

        private float timeSinceLastShot = 0f;
        private const float shootCooldown = 1f;
        private bool canShoot = true;
        public bool hasShot = false;
        public int killerID = -1;
        public int shooterID = -1;
        public bool won = false;
        private float timeSinceDeath = 0f;
        private const float respawnCooldown = 1f;
        private bool canRespawn = false;

        private KeyboardState previousKbState;
        public int bulletsToRemove = 0;
        private static Random random = new Random();
        public Color color;

        public Player(Vector2 position)
        {
            Position = position;
            isAlive = true;
            direction = Direction.Right;
            hearts = 4;
            LoadContent();
        }
        public Player()
        { // creates the player in a random position
            isAlive = true;
            direction = Direction.Right;
            hearts = 4;
            LoadContent();
            Position = GenerateRandomPosition();
        }

        public void LoadContent()
        { // Loading the textures needed
            Texture = AllUsing.content.Load<Texture2D>("figure1");
            heartTexture = AllUsing.content.Load<Texture2D>("redheartSmall");
            emptyHeartTexture = AllUsing.content.Load<Texture2D>("emptyheartSmall");
            Position = new Vector2(100, 100);
            direction = Direction.Right;
        }

        private Vector2 GenerateRandomPosition()
        { //generate a random position for the player
            int x = random.Next(0, AllUsing.screenWidth - Texture.Width);
            int y = 50;
            return new Vector2(x, y);
        }

        public void respawn()
        { //respawning the player
            Position = GenerateRandomPosition();
            isAlive = true;
            hearts = 4;
        }

        public void onGround()
        { //Is the player stepping on ground or no. updating the isOnGround bool
            if ((AllUsing.map.returnGround(new Vector2(Position.X, Position.Y + Texture.Height + 5)) == TileType.tile) ||
                (AllUsing.map.returnGround(new Vector2(Position.X + Texture.Width, Position.Y + Texture.Height + 5)) == TileType.tile) ||
                (AllUsing.map.returnGround(new Vector2(Position.X + (Texture.Width / 2), Position.Y + Texture.Height + 5)) == TileType.tile))
            {
                isOnGround = true;
            }
            else
            {
                isOnGround = false;
            }
        }

        public void died()
        { //updating when the player is dead
            isAlive = false;
            var kbState = Keyboard.GetState();
            if (kbState.IsKeyDown(Keys.Enter) && canRespawn)
                respawn();
        }

        public void wasShot(int id)
        { //if the player was shot by another player
            hearts--;
            shooterID = id;
            if (hearts <= 0)
            {
                isAlive = false;
                Position.Y += 50;
                killerID = id;
            }
            
        }
        public void CheckFallenFromMap()
        { //Kills the player if he fell from the map (touched the bottom)
            if (this.Position.Y + this.Texture.Height >= AllUsing.map.dimensions.Y - 5)
            {
                hearts -= 2;
                if (hearts <= 0)
                {
                    isAlive = false;
                }
            }
        }


        public bool canGo(Direction direction)
        { // Is there is a tile blocking the direction the player wants to move
            Rectangle futureBounds;
            if (direction == Direction.Up)
            {
                futureBounds = new Rectangle((int)Position.X, (int)(Position.Y - 1), Texture.Width - 5, Texture.Height - 5);
            }
            else if (direction == Direction.Left)
            {
                futureBounds = new Rectangle((int)(Position.X - speed * AllUsing.time), (int)Position.Y + 5, Texture.Width, Texture.Height);
            }
            else if (direction == Direction.Right)
            {
                futureBounds = new Rectangle((int)(Position.X + speed * AllUsing.time), (int)Position.Y + 5, Texture.Width, Texture.Height);
            }
            else
            {
                return false;
            }

            for (int x = futureBounds.Left; x < futureBounds.Right; x += 10)
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

        public void isStuck()
        { //fixing a bug in the game, the player sometimes gets
          //stuck in the ground without being able to move
            if (!canGo(Direction.Right) && !canGo(Direction.Left) && canGo(Direction.Up))
            {
                Position.Y -= 5;
            }
        }

        public void Update()
        { // Updates the player by the input from the keyboard and the other playes
            // checks his movement, powers gravity if needed and such
            var kbState = Keyboard.GetState();
            float deltaTime = AllUsing.time;

            if (!isAlive)
            {
                if (!canRespawn)
                {
                    timeSinceDeath += deltaTime;
                    if (timeSinceDeath >= respawnCooldown)
                    {
                        canRespawn = true;
                        timeSinceDeath = 0f;
                    }
                }
                died();
                return;
            }
            checkIfHitByBullet();
            CheckFallenFromMap();

            if (this.hearts <= 0)
            {
                isAlive = false;
                this.hearts = 0;
            }
            if (this.score >= 3)
                won = true;


            if (kbState.IsKeyDown(Keys.A))
            {
                direction = Direction.Left;
                if (canGo(direction))
                    _velocity.X = -speed;
                else
                    _velocity.X = 0;
            }
            else if (kbState.IsKeyDown(Keys.D))
            {
                direction = Direction.Right;
                if (canGo(direction))
                    _velocity.X = speed;
                else
                    _velocity.X = 0;
            }
            else
            {
                _velocity.X = 0;
            }

            onGround();

            if (kbState.IsKeyDown(Keys.W) && isOnGround && !isJumping)
            {
                StartJump();
            }
            if (isJumping)
            {
                ContinueJump(deltaTime);
            }
            else if (!isOnGround)
            {
                airTime += deltaTime;
                gravity = MathHelper.Lerp(baseGravity, maxGravity, airTime / maxAirTime);
                _velocity.Y += gravity * deltaTime;
            }
            else
            {
                airTime = 0f;
                gravity = baseGravity;
                _velocity.Y = 0;
            }
            isStuck();

            Position += _velocity * deltaTime;

            if (!canShoot)
            {
                timeSinceLastShot += deltaTime;
                if (timeSinceLastShot >= shootCooldown)
                {
                    canShoot = true;
                    timeSinceLastShot = 0f;
                }
            }
            if (kbState.IsKeyDown(Keys.Space) && !previousKbState.IsKeyDown(Keys.Space) && canShoot)
            {
                shoot(direction);
                hasShot = true;
                canShoot = false;
            }
            moveBullets();

            if (this.score >= 2)
            {
                this.won = true;
            }

            previousKbState = kbState;
        }

        private void StartJump()
        { //starting the player's jump
            isJumping = true;
            initialJumpPosition = Position;
            jumpTimeElapsed = 0;
            currentJumpSpeed = initialJumpSpeed;
        }

        private void ContinueJump(float deltaTime)
        { //Continuing the player's jump
            jumpTimeElapsed += deltaTime;
            float jumpProgress = jumpTimeElapsed / jumpDuration;
            currentJumpSpeed = MathHelper.Lerp(initialJumpSpeed, 0, jumpProgress);

            if (jumpTimeElapsed < jumpDuration && Position.Y > initialJumpPosition.Y - maxJumpHeight)
            {
                if (canGo(Direction.Up))
                {
                    _velocity.Y = -currentJumpSpeed;
                }
                else
                {
                    isJumping = false;
                    _velocity.Y = 0;
                }
            }
            else
            {
                isJumping = false;
                _velocity.Y = 0;
            }
        }

        public void moveBullets()
        { // moving all the bullets of the player
            if (bullets.Count > 0)
            {
                foreach (var bullet in bullets)
                {
                    bullet.Move();
                    if (!bullet.canGo(bullet.direction))
                        bulletsToRemove++;
                }
                for (int i = 0; i < bulletsToRemove; i++)
                {
                    if (bullets.Count > 0)
                        bullets.Dequeue();
                }
                bulletsToRemove = 0;
            }
        }

        public void checkIfHitByBullet()
        { // checking if the player was hit by other player's bullets
            foreach (var player in AllUsing.players)
            {
                if (player != this)
                {
                    foreach (var bullet in player.bullets)
                    {
                        Rectangle bulletBounds = new Rectangle((int)bullet.position.X, (int)bullet.position.Y, bullet.texture.Width, bullet.texture.Height);
                        if (Bounds.Intersects(bulletBounds))
                        {
                            player.bullets.Dequeue();
                            wasShot(player.id);
                            if (bullet.direction == Direction.Left && canGo(bullet.direction))
                            {
                                this.Position.X -= 12;
                            }
                            else if (bullet.direction == Direction.Right && canGo(bullet.direction))
                            {
                                this.Position.X += 12;
                            }
                            return;

                        }
                    }
                }
            }
        }

        public void Draw(Color color)
        { // Drawing the player in the wanted color.
            //if it's the player of the gamer it's adding the view of hearts and score
            if (!isAlive)
            {
                float rotation = (float)Math.PI / 2;
                Vector2 origin = new Vector2(Texture.Width / 2, Texture.Height / 2);

                if (direction == Direction.Right)
                {
                    AllUsing.spriteBatch.Draw(Texture, Position, null, Color.Gray, rotation, origin, 1f, SpriteEffects.None, 0f);

                }
                else if (direction == Direction.Left)
                {
                    AllUsing.spriteBatch.Draw(Texture, Position, null, Color.Gray, -rotation, origin, 1f, SpriteEffects.FlipHorizontally, 0f);
                }
                if (this == AllUsing.myPlayer && !AllUsing.myPlayer.won)
                {
                    string deathMessage = "Your player was killed. Press ENTER to respawn";
                    Vector2 promptSize = AllUsing.font.MeasureString(deathMessage);
                    Vector2 promptPosition = new Vector2(AllUsing.topLeft.X + ((AllUsing.screenWidth - promptSize.X) / 2), (AllUsing.screenHeight / 2) + AllUsing.topLeft.Y);
                    AllUsing.spriteBatch.DrawString(AllUsing.font, deathMessage, promptPosition, AllUsing.fontColor);
                    DrawStatistics();
                }

                return;
            }

            if (direction == Direction.Right)
            {
                AllUsing.spriteBatch.Draw(Texture, Position, color);
            }
            if (direction == Direction.Left)
            {
                AllUsing.spriteBatch.Draw(Texture, Position, null, color, 0f, Vector2.Zero, 1f, SpriteEffects.FlipHorizontally, 0f);
            }
            if (name != null)
            {
                Vector2 promptSize = AllUsing.font.MeasureString(name);
                Vector2 promptPosition = new Vector2(Position.X - (promptSize.X / 2) + (Texture.Width / 2), Position.Y - 25);
                AllUsing.spriteBatch.DrawString(AllUsing.font, name, promptPosition, AllUsing.fontColor);
            }

            foreach (var bullet in bullets)
            {
                bullet.Draw();
            }

            if (this == AllUsing.myPlayer)
            {
                DrawStatistics();
                Vector2 scorePosition = new Vector2(AllUsing.topLeft.X + 20, 5 + AllUsing.topLeft.Y);
                AllUsing.spriteBatch.DrawString(AllUsing.font, "score: " + this.score, scorePosition, AllUsing.fontColor);
            }
        }
        public void DrawStatistics()
        { // Drawing the hearts of the player
            Vector2 offset = new Vector2(575 + AllUsing.topLeft.X, 5 + AllUsing.topLeft.Y);
            for (int i = 4; i >= 1; i--)
            {
                Vector2 heartPosition = new Vector2(offset.X + (4 - i) * (heartTexture.Width + 6), offset.Y);
                if (i <= hearts)
                    AllUsing.spriteBatch.Draw(heartTexture, heartPosition, Color.White);
                else
                    AllUsing.spriteBatch.Draw(emptyHeartTexture, heartPosition, Color.White);
            }

        }

        public void shoot(Direction direction)
        {
            bullets.Enqueue(new Bullet(Position, direction));
        }
    }
}