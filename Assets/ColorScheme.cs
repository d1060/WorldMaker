using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class ColorScheme
{
    int index;
    List<Color32> colors = new List<Color32>();
    int imageCornerRadius = 20;
    Texture2D tex;
    Sprite sprite;
    GameObject gameObject;

    public int Index { get { return index; } set { index = value; } }
    public List<Color32> Colors { get { return colors; } set { colors = value; } }
    public GameObject GameObject { get { return gameObject; } set { gameObject = value; } }

    public void ReadBinary(BinaryReader reader)
    {
        for (int a = 0; a < 512; a++) // Each color scheme contains 512 colors.
        {
            Color32 c = new Color32();
            c = c.ReadBinary(reader);
            colors.Add(c);
        }
    }

    public void WriteBinary(BinaryWriter writer)
    {
        for (int a = 0; a < colors.Count; a++) // Each color scheme contains 512 colors.
        {
            Color32 c = colors[a];
            c.WriteBinary(writer);
        }
    }

    bool isCorner(int x, int y)
    {
        if (x > 256) x = 511 - x;
        if (y > 64) y = 127 - y;

        if (x > imageCornerRadius || y > imageCornerRadius) return false;

        int xBar = imageCornerRadius - x;
        int yBar = imageCornerRadius - y;

        float length = Mathf.Sqrt(xBar * xBar + yBar * yBar);
        if (length >= imageCornerRadius) return true;

        return false;
    }

    public Sprite GetSprite()
    {
        BuildTexture();
        return sprite;
    }

    public Texture2D GetTexture()
    {
        BuildTexture();
        return tex;
    }

    void BuildTexture()
    {
        if (tex == null)
        {
            tex = new Texture2D(512, 128, TextureFormat.RGBA32, false);

            Color[] cs = new Color[512 * 128];

            int x = 0;
            while (x < tex.width)
            {
                Color32 c = colors[x];
                int y = 0;
                while (y < tex.height)
                {
                    byte alpha = 255;
                    if (isCorner(x, y)) alpha = 0;

                    cs[y * tex.width + x] = new Color32(c.r, c.g, c.b, alpha);
                    y++;
                }
                x++;
            }
            // Upload changes to the graphics card
            tex.SetPixels(cs);
            tex.Apply();

            sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
        }
    }

    public void SetActive(bool bActive)
    {
        gameObject?.SetActive(bActive);
    }
}
