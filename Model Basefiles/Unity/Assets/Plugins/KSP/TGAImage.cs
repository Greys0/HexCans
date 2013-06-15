using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public enum TGAImageType : byte
{
    NoImage = 0,
    Uncompressed_ColorMap = 1,
    Uncompressed_TrueColor = 2,
    Uncompressed_GreyScale = 3,
    RTE = 4,
    RTE_ColorMap = 9,
    RTE_TrueColor = 10,
    RTE_GreyScale = 11
}

public class TGAHeader
{
    public int idLength;
    public bool hasColorMap;
    public TGAImageType imageType;
    public bool rteEncoding;
    public byte[] colorMap;
    public short xOrigin;
    public short yOrigin;
    public ushort width;
    public ushort height;
    public byte pixelDepth;
    public byte imageDesc;

    public int nPixels;
    public int bpp;

    public TGAHeader()
    {
    }

    public TGAHeader(byte[] data)
    {
        if (data.Length < 18)
        {
            Debug.LogError("TGA invalid length of only " + data.Length + "bytes");
            return;
        }

        idLength = (int)data[0];

        hasColorMap = !(data[1] == 0);

        imageType = (TGAImageType)data[2];
        rteEncoding = ((byte)imageType & (byte)TGAImageType.RTE) != 0;

        colorMap = new byte[5];
        for (int i = 0; i < 5; i++)
            colorMap[i] = data[i + 3];

        byte[] imageSpec = new byte[10];
        for (int i = 0; i < 10; i++)
            imageSpec[i] = data[i + 8];

        xOrigin = System.BitConverter.ToInt16(imageSpec, 0);
        yOrigin = System.BitConverter.ToInt16(imageSpec, 2);
        width = System.BitConverter.ToUInt16(imageSpec, 4);
        height = System.BitConverter.ToUInt16(imageSpec, 6);
        pixelDepth = imageSpec[8];
        imageDesc = imageSpec[9];

        nPixels = width * height;
        bpp = pixelDepth / 8;
    }

    public byte[] GetData()
    {
        byte[] data = new byte[18];

        data[2] = (byte)imageType;
        System.BitConverter.GetBytes(width).CopyTo(data, 12);
        System.BitConverter.GetBytes(height).CopyTo(data, 14);
        data[16] = pixelDepth;

        return data;
    }
}

/// <summary>
/// Mu's TGA Image Library
/// Copyright KSP 2012 - Dont nick it.
/// </summary>
public class TGAImage
{
    private Color32[] colorData;

    private TGAHeader header;


    public bool ReadImage(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError("TGA image does not exist at path '" + filePath + "'");
            return false;
        }

        return ReadImage(new FileInfo(filePath));
    }

    public bool ReadImage(FileInfo file)
    {
        byte[] data = File.ReadAllBytes(file.FullName);
        if (data == null)
        {
            Debug.LogError("TGA: data error");
            return false;
        }

        if (data.Length < 18)
        {
            Debug.LogError("TGA invalid length of only " + data.Length + "bytes");
            return false;
        }

        header = new TGAHeader(data);

        //Debug.Log(header.width + " " + header.height + " " + header.bpp + " " + header.imageType.ToString());

        colorData = ReadImage(header, data);

        if (colorData == null)
            return false;

        return true;
    }


    private Color32[] ReadImage(TGAHeader header, byte[] data)
    {
        switch (header.imageType)
        {
            case TGAImageType.RTE_TrueColor:
                return ReadRTETrueColorImage(header, data);
            case TGAImageType.Uncompressed_TrueColor:
                return ReadTrueColorImage(header, data);
            default:
                Debug.Log("Image type of " + header.imageType.ToString() + " is not supported.");
                return null;
        }
    }

    private Color32[] ReadTrueColorImage(TGAHeader header, byte[] data)
    {
        int bpp = header.pixelDepth / 8;
        bool hasAlpha = (bpp == 4);
        Color32[] colors = new Color32[header.width * header.height];
        int index = 18; // initial offset
        int colorIndex = 0;
        Color32 pixelColor = new Color32(255, 255, 255, 255);

        for (int y = 0; y < header.height; y++)
        {
            for (int x = 0; x < header.width; x++)
            {
                if (hasAlpha)
                {
                    pixelColor.b = data[index++];
                    pixelColor.g = data[index++];
                    pixelColor.r = data[index++];
                    pixelColor.a = data[index++];
                }
                else
                {
                    pixelColor.b = data[index++];
                    pixelColor.g = data[index++];
                    pixelColor.r = data[index++];
                }

                colors[colorIndex++] = pixelColor;
            }
        }

        return colors;
    }

    private Color32[] ReadRTETrueColorImage(TGAHeader header, byte[] data)
    {
        int bpp = header.pixelDepth / 8;
        bool hasAlpha = (bpp == 4);

        Color32[] colors = new Color32[header.width * header.height];
        int index = 18; // initial offset
        int colorIndex = 0;
        Color32 pixelColor = new Color32(255, 255, 255, 255);

        byte pixelRun;
        byte i;

        while (colorIndex < header.nPixels)
        {
            pixelRun = data[index++];

            if ((pixelRun & 128) != 0)
            {
                // this is a RLE packet so we need to remove the last bit (-128) and add one to the count to get the number of pixels
                pixelRun = (byte)((pixelRun - 128) + 1);

                if (hasAlpha)
                {
                    pixelColor.b = data[index++];
                    pixelColor.g = data[index++];
                    pixelColor.r = data[index++];
                    pixelColor.a = data[index++];
                }
                else
                {
                    pixelColor.b = data[index++];
                    pixelColor.g = data[index++];
                    pixelColor.r = data[index++];
                }

                for (i = 0; i < pixelRun; i++)
                {
                    colors[colorIndex++] = pixelColor;
                }
            }
            else
            {
                // this is not an RLE packet since last bit is not set. just add one to get count of pixels
                pixelRun = (byte)(pixelRun + 1);

                for (i = 0; i < pixelRun; i++)
                {
                    if (hasAlpha)
                    {
                        pixelColor.b = data[index++];
                        pixelColor.g = data[index++];
                        pixelColor.r = data[index++];
                        pixelColor.a = data[index++];
                    }
                    else
                    {
                        pixelColor.b = data[index++];
                        pixelColor.g = data[index++];
                        pixelColor.r = data[index++];
                    }

                    colors[colorIndex++] = pixelColor;
                }
            }
        }

        return colors;
    }



    public Texture2D CreateTexture()
    {
        return CreateTexture(true, true, true, false, false);
    }

    public Texture2D CreateTexture(bool mipmap, bool linear, bool compress, bool compressHighQuality, bool allowRead)
    {
        if (header == null)
        {
            Debug.Log("Cannot create texture: No header created");
            return null;
        }

        if (colorData == null)
        {
            Debug.Log("Cannot create texture: No color data present");
            return null;
        }

        Texture2D newTex = null;

        if (header.bpp == 4)
        {
            newTex = new Texture2D(header.width, header.height, TextureFormat.RGBA32, mipmap, linear);
        }
        else if (header.bpp == 3)
        {
            newTex = new Texture2D(header.width, header.height, TextureFormat.RGB24, mipmap, linear);
        }

        if (newTex == null)
        {
            Debug.Log("Cannot create texture: Header denotes incorrect format");
            return null;
        }

        newTex.SetPixels32(colorData);
        newTex.Apply(mipmap);

        if (compressHighQuality)
        {
            newTex.Compress(compressHighQuality);

            newTex.Apply(mipmap, !allowRead);
        }

        return newTex;
    }
}