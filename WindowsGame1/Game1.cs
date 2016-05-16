using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

using System.Collections;

namespace HardestGameEver
{

    public class Wall
    {
        Rectangle rect;

        public Wall(int x, int y, int width, int height)
        {
            rect = new Rectangle(x, y, width, height);
        }

        public Rectangle getRect()
        {
            return rect;
        }

    }

    public class Enemy
    {

        Rectangle rect;
        bool xAxis; // true if moving along x-axis, false if along y-axis
        bool flipDir; // true if direction is opposite of starting direction

        int moveLengthBeforeFlipping;
        int movedLength = 0;

        public int MOVE_SPEED;

        public Enemy(int x, int y, bool axis, int length, int speed, bool startLeft)
        {
            flipDir = (startLeft) ? true : false;
            MOVE_SPEED = speed;
            this.moveLengthBeforeFlipping = length;
            this.xAxis = axis;
            rect = new Rectangle(x, y, 20, 20);
        }

        public Rectangle getRect()
        {
            return rect;
        }

        public void update()
        {
            int modDirection = (flipDir) ? -1 : 1;
            if (xAxis)
            {
                // move along x axis
                rect.X += MOVE_SPEED * modDirection;
            }
            else
            {
                // move along y axis
                rect.Y += MOVE_SPEED * modDirection;
            }
            movedLength += MOVE_SPEED;
            if (movedLength > moveLengthBeforeFlipping)
            {
                movedLength = 0;
                flipDir = !flipDir;
            }
        }
    }

    public class Player
    {

        Rectangle rect;
        public static int MOVE_SPEED = 5;

        public bool isGravity = false;
        public static double GRAVITY = 0.8;
        double yvelocity = 0;
        bool isOnGround = false;
        bool spaceHasComeUp = true;

        public Player(int x, int y)
        {
            rect = new Rectangle(x, y, 35, 35);
        }

        public int getX()
        {
            return rect.X;
        }

        public int getY()
        {
            return rect.Y;
        }

        public void setPos(int x, int y)
        {
            rect.X = x;
            rect.Y = y;
        }

        public Rectangle getRect()
        {
            return this.rect;
        }

        public bool collisionDetected(int x, int y, MainGame g)
        {
            Rectangle attempt = new Rectangle(x, y, 35, 35);
            foreach (Wall w in g.walls)
            {
                if (w.getRect().Intersects(attempt))
                {
                    return true;
                }
            }
            foreach (LaserWall l in g.laserwalls)
            {
                if (l.isBlue && !isGravity)
                {
                    if (l.rect.Intersects(attempt))
                    {
                        return true;
                    }
                }
                else if (!l.isBlue && isGravity)
                {
                    if (l.rect.Intersects(attempt))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void update(KeyboardState keys, MainGame g)
        {
            int move_x = 0;
            int move_y = 0;

            if (keys.IsKeyDown(Keys.R))
            {
                g.loadLevel();
            }

            if (isGravity)
            {
                yvelocity += GRAVITY;
            }

            if (keys.IsKeyUp(Keys.Space))
            {
                spaceHasComeUp = true;
            }

            if (keys.IsKeyDown(Keys.Space) && spaceHasComeUp)
            {
                spaceHasComeUp = false;
                isGravity = !isGravity;
                Console.WriteLine(isGravity);
                isOnGround = false;
            }

            if (keys.IsKeyDown(Keys.D))
            {
                move_x += MOVE_SPEED;
            }
            else if (keys.IsKeyDown(Keys.A))
            {
                move_x -= MOVE_SPEED;
            }
            if (keys.IsKeyDown(Keys.W))
            {
                if (!isGravity)
                {
                    move_y -= MOVE_SPEED;
                }
                else
                {
                    if (isOnGround)
                    {
                        yvelocity = -15;
                    }
                }
            }
            else if (keys.IsKeyDown(Keys.S))
            {
                if (!isGravity)
                {
                    move_y += MOVE_SPEED;
                }
            }

            if (!collisionDetected(rect.X + move_x, rect.Y, g))
            {
                rect.X += move_x;
            }

            if (!isGravity)
            {
                if (!collisionDetected(rect.X, rect.Y + move_y, g))
                {
                    rect.Y += move_y;
                }
            }
            else
            {
                if (!collisionDetected(rect.X, rect.Y + (int)yvelocity, g))
                {
                    rect.Y += (int)yvelocity;
                    isOnGround = false;
                }
                else
                {
                    yvelocity = 0;
                    isOnGround = true;
                }
            }

        }

    }

    public class LaserWall
    {

        public Rectangle rect;
        public bool isBlue;

        // blue lasers stop non-gravity, red lasers stop gravity
        public LaserWall(int x, int y, int width, int height, bool isBlue)
        {
            rect = new Rectangle(x, y, width, height);
            this.isBlue = isBlue;
        }
    }

    public class MainGame : Microsoft.Xna.Framework.Game
    {

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        SpriteFont font;

        public ArrayList enemies = new ArrayList();
        public ArrayList walls = new ArrayList();
        public ArrayList laserwalls = new ArrayList();

        Texture2D enemyText;
        Texture2D playerText;
        Texture2D playerBlueText;
        Texture2D laser_r;
        Texture2D laser_b;
        Texture2D winText;
        Texture2D wallText;
        Texture2D fadeText;

        Rectangle fadeRect;

        Random r = new Random();

        KeyboardState oldkeys = Keyboard.GetState();

        Player player = new Player(50, 360);
        bool dead = false;
        bool beat_game = false;
        int MAX_LEVEL = 3;

        Rectangle winRect = new Rectangle(650, 350, 50, 50);

        public int LEVEL = 1;

        int fadeAlpha = 0;
        bool fadeIn = true;
        int FADE_SPEED = 10;

        bool dead_resetting;

        public MainGame()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.IsFullScreen = false;
            graphics.PreferredBackBufferHeight = 450;
            graphics.PreferredBackBufferWidth = 750;
            Content.RootDirectory = "Content";
        }

        //////////////////////// LEVELS START /////////////////////////
        // WHEN ADDING A LEVEL, REMEMBER TO CHANGE MAX_LEVEL

        private void l1()
        {

            player.setPos(50, 350);

            // enemies
            enemies.Add(new Enemy(75, 125, true, 175, 5, false));
            enemies.Add(new Enemy(250, 225, true, 175, 5, true));
            enemies.Add(new Enemy(75, 325, true, 175, 5, false));

            enemies.Add(new Enemy(375, 125, true, 175, 5, false));
            enemies.Add(new Enemy(550, 225, true, 175, 5, true));
            enemies.Add(new Enemy(375, 325, true, 175, 5, false));

            // walls
            walls.Add(new Wall(0, 0, 50, 400)); // left
            walls.Add(new Wall(0, 400, 700, 49)); // bottom
            walls.Add(new Wall(700, 0, 49, 449)); // right
            walls.Add(new Wall(49, 0, 700, 50)); // top
            walls.Add(new Wall(300, 125, 50, 275));
            walls.Add(new Wall(600, 49, 100, 301));
        }

        private void l2()
        {

            player.setPos(50, 50);

            // enemies
            enemies.Add(new Enemy(200, 75, false, 285, 10, false));
            enemies.Add(new Enemy(400, 75, false, 285, 5, false));
            enemies.Add(new Enemy(450, 360, false, 285, 5, true));

            enemies.Add(new Enemy(65, 100, true, 100, 3, false));
            enemies.Add(new Enemy(165, 200, true, 100, 3, true));
            enemies.Add(new Enemy(65, 300, true, 100, 3, false));

            enemies.Add(new Enemy(550, 60, true, 100, 3, false));

            // walls
            walls.Add(new Wall(0, 0, 50, 400)); // left
            walls.Add(new Wall(0, 400, 700, 49)); // bottom
            walls.Add(new Wall(700, 0, 49, 449)); // right
            walls.Add(new Wall(49, 0, 700, 50)); // top

            walls.Add(new Wall(250, 50, 125, 250));
            walls.Add(new Wall(500, 100, 75, 300));
        }

        private void l3()
        {
            player.setPos(100, 300);

            walls.Add(new Wall(0, 0, 50, 400)); // left
            walls.Add(new Wall(0, 400, 700, 49)); // bottom
            walls.Add(new Wall(700, 0, 49, 449)); // right
            walls.Add(new Wall(49, 0, 700, 50)); // top

            walls.Add(new Wall(200, 225, 50, 100));
            walls.Add(new Wall(450, 225, 50, 275));

            laserwalls.Add(new LaserWall(250, 0, 200, 500, true));
        }

        //////////////////////// LEVELS END /////////////////////////

        public void loadLevel()
        {
            // empty lists
            enemies.Clear();
            walls.Clear();
            laserwalls.Clear();

            // initiate corresponding level
            switch (LEVEL)
            {
                case 1:
                    l1();
                    break;
                case 2:
                    l2();
                    break;
                case 3:
                    l3();
                    break;
            }
        }


        protected override void Initialize()
        {
            loadLevel();
            fadeRect = new Rectangle(0, 0, GraphicsDevice.Viewport.Bounds.Width, GraphicsDevice.Viewport.Bounds.Height);
            base.Initialize();
        }


        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            wallText = Content.Load<Texture2D>("wall");
            enemyText = Content.Load<Texture2D>("enemy");
            playerText = Content.Load<Texture2D>("player");
            playerBlueText = Content.Load<Texture2D>("player_blue");
            laser_b = Content.Load<Texture2D>("laserb");
            laser_r = Content.Load<Texture2D>("laserr");
            winText = Content.Load<Texture2D>("win");
            fadeText = Content.Load<Texture2D>("fade");
            font = Content.Load<SpriteFont>("SpriteFont1");
        }


        protected override void UnloadContent()
        {

        }

        private bool checkWin()
        {
            return (player.getRect().Intersects(winRect));
        }


        protected override void Update(GameTime gameTime)
        {

            if (checkWin() && fadeAlpha == 0)
            {
                if (LEVEL == MAX_LEVEL)
                {
                    beat_game = true;
                }
                else
                {
                    fadeAlpha = 1;
                    fadeIn = true;
                }
            }

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            KeyboardState keys = Keyboard.GetState();
            if (!dead && !beat_game && !dead_resetting)
            {
                if (fadeAlpha == 0)
                {
                    player.update(keys, this);
                }

                // Update Enemies
                foreach (Enemy e in enemies)
                {
                    e.update();

                }

                // Check for Enemy-Player collisions
                foreach (Enemy e in enemies)
                {
                    if (e.getRect().Intersects(player.getRect()))
                    {
                        dead = true;
                    }
                }
            }
            else if (dead)
            {
                if (!dead_resetting)
                {
                    fadeAlpha = 1;
                    fadeIn = true;
                    LEVEL--; // kind of a cheat, but it gets countered in the 'if (fadeAlpha > 0)' in this.Draw
                    dead = false;
                    dead_resetting = true;
                }
            }

            oldkeys = keys;

            base.Update(gameTime);
        }


        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.White);
            spriteBatch.Begin();

            foreach (LaserWall l in laserwalls)
            {
                if (l.isBlue)
                {
                    spriteBatch.Draw(laser_b, l.rect, Color.White);
                }
                else
                {
                    spriteBatch.Draw(laser_r, l.rect, Color.White);
                }
            }

            foreach (Wall w in walls)
            {
                spriteBatch.Draw(wallText, w.getRect(), Color.White);
            }

            spriteBatch.Draw(winText, winRect, Color.White);

            foreach (Enemy e in enemies)
            {
                spriteBatch.Draw(enemyText, e.getRect(), Color.White);
            }

            if (player.isGravity)
            {
                spriteBatch.Draw(playerBlueText, player.getRect(), Color.White);
            }
            else
            {
                spriteBatch.Draw(playerText, player.getRect(), Color.White);
            }

            Vector2 level = new Vector2(0, 0);
            spriteBatch.DrawString(font, LEVEL.ToString(), level, Color.Red);

            if (beat_game)
            {
                Vector2 loseText = new Vector2(250, 200);
                spriteBatch.DrawString(font, "YOU BEAT THE GAME!", loseText, Color.Red);
            }

            if (fadeAlpha > 0)
            {
                spriteBatch.Draw(fadeText, fadeRect, new Color(255, 255, 255, (byte)MathHelper.Clamp(fadeAlpha, 0, 255)));
                if (fadeIn)
                {
                    fadeAlpha += FADE_SPEED;
                    if (fadeAlpha >= 255)
                    {
                        dead_resetting = false;
                        LEVEL++;
                        loadLevel();
                        fadeIn = false;
                    }
                }
                else
                {
                    fadeAlpha -= FADE_SPEED;
                }
            }
            else
            {
                fadeAlpha = 0;
            }

            spriteBatch.End();



            base.Draw(gameTime);
        }
    }
}
