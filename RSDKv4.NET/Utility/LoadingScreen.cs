using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RSDKv4.Utility;

public class LoadingScreen
{
    private SpriteBatch spriteBatch;
    private Texture2D texture;
    private MiniEngine miniEngine;

    private MiniEntity[] characters = new MiniEntity[2];

    private float loadFrames = 0;
    private float previousLoadProgress = 0.0f;

    private static readonly string[] CHARACTERS = new string[4] { "Sonic", "Tails", "Knuckles", "Sonic" };

    public LoadingScreen(Game game, GraphicsDevice device)
    {
        spriteBatch = new SpriteBatch(device);
        texture = new Texture2D(device, 1, 1);
        texture.SetData(new[] { Color.White });

        miniEngine = new MiniEngine(game, spriteBatch, "DataS2.rsdk");

        var x = FastMath.Rand(CHARACTERS.Length);
        characters[0] = miniEngine.CreateEntity($"{CHARACTERS[x]}.ani", "Running");
        characters[0].x = RSDKv4Game.WIDTH - 90;
        characters[0].y = RSDKv4Game.HEIGHT - 100;

        if (x == 3) // Sonic & Tails
        {
            characters[1] = miniEngine.CreateEntity($"Tails.ani", "Running");
            characters[1].x = RSDKv4Game.WIDTH - 160;
            characters[1].y = RSDKv4Game.HEIGHT - 100;
        }
    }

    public void Draw(GameTime gameTime)
    {
        if (loadFrames > 20)
        {
            previousLoadProgress = EaseOutCirc(previousLoadProgress, RSDKv4Game.loadPercent, 2.5f * (float)gameTime.ElapsedGameTime.TotalSeconds);

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone);

            for (int i = characters.Length - 1; i >= 0; i--)
                characters[i]?.Draw(spriteBatch);

            spriteBatch.Draw(texture, new Rectangle(0, RSDKv4Game.HEIGHT - 16, (int)(RSDKv4Game.WIDTH * previousLoadProgress), 16), new Color(0x00, 0x21, 0xc6));
            spriteBatch.End();
        }

        loadFrames++;
    }

    private static float EaseOutCirc(float from, float to, float t)
    {
        t = t > 1 ? 1 : t;
        t = (float)Math.Sqrt(1 - Math.Pow(t - 1, 2));
        return to * t + from * (1F - t);
    }
}
