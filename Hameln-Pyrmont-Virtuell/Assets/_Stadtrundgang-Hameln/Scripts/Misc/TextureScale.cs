﻿// Initial version at: http://wiki.unity3d.com/index.php/TextureScale#TextureScale.cs
// Modified by ftc to work with raw texture data (it consumes less memory).
// Currently works only on RGB24 and RGBA32 textures.

using System;
using System.Threading;
using Unity.Collections;
using UnityEngine;

public class TextureScale
{
	public class ThreadData
	{
		public int start;
		public int end;
		public ThreadData(int s, int e)
		{
			start = s;
			end = e;
		}
	}

	private static NativeArray<byte> texBytes;
	private static TextureFormat texFormat;
	private static Color32[] newColors;
	private static int h;
	private static int w;
	private static float ratioX;
	private static float ratioY;
	private static int w2;
	private static int finishCount;
	private static Mutex mutex;

	public static void Point(Texture2D tex, int newWidth, int newHeight)
	{
		ThreadedScale(tex, newWidth, newHeight, false);
	}

	public static void Bilinear(Texture2D tex, int newWidth, int newHeight)
	{
		ThreadedScale(tex, newWidth, newHeight, true);
	}

	private static void ThreadedScale(Texture2D tex, int newWidth, int newHeight, bool useBilinear)
	{
		texBytes = tex.GetRawTextureData<byte>();
		texFormat = tex.format;
		if (texFormat != TextureFormat.RGB24 && texFormat != TextureFormat.RGBA32 && texFormat != TextureFormat.ARGB32)
		{
			Debug.Log("NotImplementedException texFormat " + texFormat);
			throw new NotImplementedException();
		}
		if (newColors == null || newColors.Length < newWidth * newHeight)
		{
			newColors = new Color32[newWidth * newHeight];
		}
		if (useBilinear)
		{
			ratioX = 1.0f / ((float)newWidth / (tex.width - 1));
			ratioY = 1.0f / ((float)newHeight / (tex.height - 1));
		}
		else
		{
			ratioX = ((float)tex.width) / newWidth;
			ratioY = ((float)tex.height) / newHeight;
		}
		w = tex.width;
		h = tex.height;
		w2 = newWidth;
		var cores = Mathf.Min(SystemInfo.processorCount, newHeight);
		var slice = newHeight / cores;

		finishCount = 0;
		if (mutex == null)
		{
			mutex = new Mutex(false);
		}
		if (cores > 1)
		{
			int i = 0;
			ThreadData threadData;
			for (i = 0; i < cores - 1; i++)
			{
				threadData = new ThreadData(slice * i, slice * (i + 1));
				ParameterizedThreadStart ts = useBilinear ? new ParameterizedThreadStart(BilinearScale) : new ParameterizedThreadStart(PointScale);
				Thread thread = new Thread(ts);
				thread.Start(threadData);
			}
			threadData = new ThreadData(slice * i, newHeight);
			if (useBilinear)
			{
				BilinearScale(threadData);
			}
			else
			{
				PointScale(threadData);
			}
			while (finishCount < cores)
			{
				Thread.Sleep(1);
			}
		}
		else
		{
			ThreadData threadData = new ThreadData(0, newHeight);
			if (useBilinear)
			{
				BilinearScale(threadData);
			}
			else
			{
				PointScale(threadData);
			}
		}

		tex.Reinitialize(newWidth, newHeight);
		var newBytes = tex.GetRawTextureData<byte>();
		for (int i = 0; i < newWidth * newHeight; i++)
		{
			var color = newColors[i];
			if (tex.format == TextureFormat.RGB24)
			{
				int s = 3 * i;
				newBytes[s] = color.r;
				newBytes[s + 1] = color.g;
				newBytes[s + 2] = color.b;
			}
			else if (tex.format == TextureFormat.RGBA32)
			{
				int s = 4 * i;
				newBytes[s] = color.r;
				newBytes[s + 1] = color.g;
				newBytes[s + 2] = color.b;
				newBytes[s + 3] = color.a;
			}
            else if (tex.format == TextureFormat.ARGB32)
            {
                //int s = 4 * i;
                //newBytes[s] = color.r;
                //newBytes[s + 1] = color.g;
                //newBytes[s + 2] = color.b;
                //newBytes[s + 3] = color.a;

                int s = 4 * i;
                newBytes[s] = color.a;
                newBytes[s + 1] = color.r;
                newBytes[s + 2] = color.g;
                newBytes[s + 3] = color.b;
            }
        }
		tex.Apply();

		//newColors = null;
	}

	private static Color32 GetColor(int position)
	{
		if (texFormat == TextureFormat.RGB24)
		{
			int s = (int)position * 3;
			Color32 result = new Color32();
			result.r = texBytes[s];
			result.g = texBytes[s + 1];
			result.b = texBytes[s + 2];
			result.a = 0;
			return result;
		}
		else if (texFormat == TextureFormat.RGBA32)
		{
			int s = (int)position * 4;
			Color32 result = new Color32();
			result.r = texBytes[s];
			result.g = texBytes[s + 1];
			result.b = texBytes[s + 2];
			result.a = texBytes[s + 3];
			return result;
		}
        else if (texFormat == TextureFormat.ARGB32)
        {
            //int s = (int)position * 4;
            //Color32 result = new Color32();
            //result.r = texBytes[s];
            //result.g = texBytes[s + 1];
            //result.b = texBytes[s + 2];
            //result.a = texBytes[s + 3];
            //return result;

            int s = (int)position * 4;
            Color32 result = new Color32();
            result.a = texBytes[s];
            result.r = texBytes[s + 1];
            result.g = texBytes[s + 2];
            result.b = texBytes[s + 3];
            return result;
        }
        else throw new NotImplementedException();
	}

	public static void BilinearScale(System.Object obj)
	{
		ThreadData threadData = (ThreadData)obj;
		for (var y = threadData.start; y < threadData.end; y++)
		{
			int yFloor = (int)Mathf.Floor(y * ratioY);
			while (yFloor + 1 >= h) yFloor--;
			var y1 = yFloor * w;
			var y2 = (yFloor + 1) * w;
			var yw = y * w2;

			for (var x = 0; x < w2; x++)
			{
				int xFloor = (int)Mathf.Floor(x * ratioX);
				var xLerp = x * ratioX - xFloor;
				var c1 = ColorLerpUnclamped(GetColor(y1 + xFloor), GetColor(y1 + xFloor + 1), xLerp);
				var c2 = ColorLerpUnclamped(GetColor(y2 + xFloor), GetColor(y2 + xFloor + 1), xLerp);
				newColors[yw + x] = ColorLerpUnclamped(c1, c2, y * ratioY - yFloor);
			}
		}
		mutex.WaitOne();
		finishCount++;
		mutex.ReleaseMutex();
	}

	public static void PointScale(System.Object obj)
	{
		ThreadData threadData = (ThreadData)obj;
		for (var y = threadData.start; y < threadData.end; y++)
		{
			var thisY = (int)(ratioY * y) * w;
			var yw = y * w2;
			for (var x = 0; x < w2; x++)
			{
				newColors[yw + x] = GetColor((int)(thisY + ratioX * x));
			}
		}

		mutex.WaitOne();
		finishCount++;
		mutex.ReleaseMutex();
	}

	private static Color32 ColorLerpUnclamped(Color32 c1, Color32 c2, float value)
	{
		return new Color32((byte)(c1.r + (c2.r - c1.r) * value),
		(byte)(c1.g + (c2.g - c1.g) * value),
		(byte)(c1.b + (c2.b - c1.b) * value),
		(byte)(c1.a + (c2.a - c1.a) * value));
	}
}

/*
// Only works on ARGB32, RGB24 and Alpha8 textures that are marked readable
 
using System.Threading;
using UnityEngine;
 
public class TextureScale
{
	public class ThreadData
	{
		public int start;
		public int end;
		public ThreadData (int s, int e) {
			start = s;
			end = e;
		}
	}
 
	private static Color[] texColors;
	private static Color[] newColors;
	private static int w;
	private static float ratioX;
	private static float ratioY;
	private static int w2;
	private static int finishCount;
	private static Mutex mutex;
 
	public static void Point (Texture2D tex, int newWidth, int newHeight)
	{
		ThreadedScale (tex, newWidth, newHeight, false);
	}
 
	public static void Bilinear (Texture2D tex, int newWidth, int newHeight)
	{
		ThreadedScale (tex, newWidth, newHeight, true);
	}
 
	private static void ThreadedScale (Texture2D tex, int newWidth, int newHeight, bool useBilinear)
	{
		texColors = tex.GetPixels();
		newColors = new Color[newWidth * newHeight];
		if (useBilinear)
		{
			ratioX = 1.0f / ((float)newWidth / (tex.width-1));
			ratioY = 1.0f / ((float)newHeight / (tex.height-1));
		}
		else {
			ratioX = ((float)tex.width) / newWidth;
			ratioY = ((float)tex.height) / newHeight;
		}
		w = tex.width;
		w2 = newWidth;
		var cores = Mathf.Min(SystemInfo.processorCount, newHeight);
		var slice = newHeight/cores;
 
		finishCount = 0;
		if (mutex == null) {
			mutex = new Mutex(false);
		}
		if (cores > 1)
		{
			int i = 0;
			ThreadData threadData;
			for (i = 0; i < cores-1; i++) {
				threadData = new ThreadData(slice * i, slice * (i + 1));
				ParameterizedThreadStart ts = useBilinear ? new ParameterizedThreadStart(BilinearScale) : new ParameterizedThreadStart(PointScale);
				Thread thread = new Thread(ts);
				thread.Start(threadData);
			}
			threadData = new ThreadData(slice*i, newHeight);
			if (useBilinear)
			{
				BilinearScale(threadData);
			}
			else
			{
				PointScale(threadData);
			}
			while (finishCount < cores)
			{
				Thread.Sleep(1);
			}
		}
		else
		{
			ThreadData threadData = new ThreadData(0, newHeight);
			if (useBilinear)
			{
				BilinearScale(threadData);
			}
			else
			{
				PointScale(threadData);
			}
		}
 
		tex.Reinitialize(newWidth, newHeight);
		tex.SetPixels(newColors);
		tex.Apply();
 
		texColors = null;
		newColors = null;
	}
 
	public static void BilinearScale (System.Object obj)
	{
		ThreadData threadData = (ThreadData) obj;
		for (var y = threadData.start; y < threadData.end; y++)
		{
			int yFloor = (int)Mathf.Floor(y * ratioY);
			var y1 = yFloor * w;
			var y2 = (yFloor+1) * w;
			var yw = y * w2;
 
			for (var x = 0; x < w2; x++) {
				int xFloor = (int)Mathf.Floor(x * ratioX);
				var xLerp = x * ratioX-xFloor;
				newColors[yw + x] = ColorLerpUnclamped(ColorLerpUnclamped(texColors[y1 + xFloor], texColors[y1 + xFloor+1], xLerp),
					ColorLerpUnclamped(texColors[y2 + xFloor], texColors[y2 + xFloor+1], xLerp),
					y*ratioY-yFloor);
			}
		}
 
		mutex.WaitOne();
		finishCount++;
		mutex.ReleaseMutex();
	}
 
	public static void PointScale (System.Object obj)
	{
		ThreadData threadData = (ThreadData) obj;
		for (var y = threadData.start; y < threadData.end; y++)
		{
			var thisY = (int)(ratioY * y) * w;
			var yw = y * w2;
			for (var x = 0; x < w2; x++) {
				newColors[yw + x] = texColors[(int)(thisY + ratioX*x)];
			}
		}
 
		mutex.WaitOne();
		finishCount++;
		mutex.ReleaseMutex();
	}
 
	private static Color ColorLerpUnclamped (Color c1, Color c2, float value)
	{
		return new Color (c1.r + (c2.r - c1.r)*value, 
			c1.g + (c2.g - c1.g)*value, 
			c1.b + (c2.b - c1.b)*value, 
			c1.a + (c2.a - c1.a)*value);
	}
}

*/