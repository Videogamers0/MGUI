using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGUI.Shared.Helpers
{
    public static class ContentUtils
    {
        public static readonly Dictionary<string, Texture2D> SolidColorTextures = new Dictionary<string, Texture2D>();
        public static Texture2D GetSolidColorTexture(GraphicsDevice GraphicsDevice, Color Color)
        {
            string Key = Color.ToString();
            if (SolidColorTextures.ContainsKey(Key))
            {
                if (SolidColorTextures[Key].IsDisposed)
                    throw new Exception($"SolidColorTexture for color {Key} is disposed.");
                return SolidColorTextures[Key];
            }
            else
            {
                SolidColorTexture Texture = new SolidColorTexture(GraphicsDevice, Color);
                SolidColorTextures.Add(Key, Texture);
                return Texture;
            }
        }

        private static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();
        /// <param name="TexturePath">The full path, starting from but not including the Root 'Content'folder.
        /// <para/>This path should not contain the file extension.<para/>
        /// EX: @"TileSets\Environment\set1"</param>
        /// <returns></returns>
        public static Texture2D GetTexture(ContentManager ContentManager, string TexturePath)
        {
            if (Textures.ContainsKey(TexturePath))
            {
                if (Textures[TexturePath].IsDisposed)
                    throw new Exception($"Texture '{TexturePath}' is disposed.");
                return Textures[TexturePath];
            }
            else
            {
                Texture2D Texture = ContentManager.Load<Texture2D>(TexturePath);
                if (Texture == null)
                    throw new ArgumentException($"No texture at '{TexturePath}' was found");
                Textures.Add(TexturePath, Texture);
                return Texture;
            }
        }

        private static readonly Dictionary<string, Effect> Effects = new Dictionary<string, Effect>();
        public static Effect GetEffect(ContentManager ContentManager, string EffectName)
        {
            if (Effects.ContainsKey(EffectName))
            {
                if (Effects[EffectName].IsDisposed)
                    throw new Exception($"Effect {EffectName} is disposed.");
                return Effects[EffectName];
            }
            else
            {
                Effect Effect = ContentManager.Load<Effect>(EffectName);
                if (Effect == null)
                    throw new ArgumentException($"No effect '{EffectName}' was found");
                Effects.Add(EffectName, Effect);
                return Effect;
            }
        }
    }
}
