// Created by Carlos Arturo Rodriguez Silva (Legend)
// Contact: carlosarturors@gmail.com

using UnityEngine;
using System.IO;
using System;


namespace RhythmVisualizatorPro
{
    public static class ImportImage
    {

        public static string lastName;
        public static string lastImageLocation;
        public static long lastFileSize;

        //public static UnityEngine.Color GetDominantColor (string path) {
        //	Bitmap b = (Bitmap)System.Drawing.Image.FromFile (path, true);

//	System.Drawing.Color color = ColorResources.GetDominantColor (b);

//	string colorHex = ColorTranslator.ToHtml (color);

//	return ColorResources.HexToColor (colorHex);
//}

//public static Bitmap GetBitmapFromPath (string path) {
//	Bitmap b = (Bitmap)System.Drawing.Image.FromFile (path, true);

//	return b;
//}

#if !UNITY_WEBGL
        public static Sprite LoadNewSprite(string FilePath, float PixelsPerUnit = 100.0f)
        {

            var file = TagLib.File.Create(FilePath);
            

            try {
                TagLib.IPicture pic = file.Tag.Pictures[0];  //pic contains data for image.

                // Load a PNG or JPG image from disk to a Texture2D, assign this texture to a new sprite and return its reference
                Sprite NewSprite;
                Texture2D SpriteTexture = new Texture2D(1280, 720);
                SpriteTexture.LoadImage(pic.Data.Data);

                NewSprite = Sprite.Create(SpriteTexture, new Rect(0, 0, SpriteTexture.width, SpriteTexture.height), new Vector2(0, 0), PixelsPerUnit);

                return NewSprite;
            }
            catch {


            }

            Sprite defaultImage = Resources.Load("No Album Art Background", typeof(Sprite)) as Sprite;

            return defaultImage;
        }
#endif

        public static Sprite LoadNewSprite(Texture2D texture, float PixelsPerUnit = 100.0f)
        {

            // Load a PNG or JPG image from disk to a Texture2D, assign this texture to a new sprite and return its reference
            Sprite NewSprite;
            Texture2D SpriteTexture = texture;
            NewSprite = Sprite.Create(SpriteTexture, new Rect(0, 0, SpriteTexture.width, SpriteTexture.height), new Vector2(0, 0), PixelsPerUnit);


            return NewSprite;
        }


        public static Texture2D LoadTexture(string FilePath)
        {

            // Load a PNG or JPG file from disk to a Texture2D
            // Returns null if load fails

            Texture2D Tex2D;
            byte[] FileData;

            if (System.IO.File.Exists(FilePath)) {
                FileData = System.IO.File.ReadAllBytes(FilePath);
                Tex2D = new Texture2D(2, 2);           // Create new "empty" texture
                if (Tex2D.LoadImage(FileData))           // Load the imagedata into the texture (size is set automatically)
                    return Tex2D;                 // If data = readable -> return texture
            }
            return null;                     // Return null if load failed
        }
    }
}

