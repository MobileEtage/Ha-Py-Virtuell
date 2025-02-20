using UnityEngine;

//-----------------------------------------------------------------------------
// Copyright 2012-2022 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProMovieCapture
{
	/// <summary>
	/// Capture from a WebCamTexture object
	/// </summary>
	[AddComponentMenu("AVPro Movie Capture/Capture From WebCamTexture", 3)]
	public class CaptureFromWebCamTexture : CaptureFromTexture
	{
#if AVPRO_MOVIECAPTURE_WEBCAMTEXTURE_SUPPORT
		private WebCamTexture _webcam = null;
		private float _webCamFps = 0;
		private int _webCamFrame = 0;
		private float _webCamStartTime = 0;

		public WebCamTexture WebCamTexture
		{
			get { return _webcam; }
			set { _webcam = value; SetSourceTexture(_webcam); }
		}

		public float WebCamFPS
		{
			get
			{
				return _webCamFps;
			}
		}

		public override void Start()
		{
#if !UNITY_EDITOR && UNITY_IOS
			_isTopDown = false;
#endif
			base.Start();
		}

		public override void UpdateFrame()
		{
			// WebCamTexture doesn't update every Unity frame
			if (_webcam != null && _webcam.didUpdateThisFrame)
			{
				_webCamFrame++;
				float timeNow = Time.realtimeSinceStartup;
				float timeDelta = timeNow - _webCamStartTime;
				if (timeDelta >= 1.0f)
				{
					_webCamFps = (float)_webCamFrame / timeDelta;
					_webCamFrame = 0;
					_webCamStartTime = timeNow;
				}
				UpdateSourceTexture();

			}

			base.UpdateFrame();
		}
#else
		public override void Start()
		{
			Debug.LogError("[AVProMovieCapture] To use WebCamTexture capture component/demo you must add the string AVPRO_MOVIECAPTURE_WEBCAMTEXTURE_SUPPORT must be added to `Scriping Define Symbols` in `Player Settings > Other Settings > Script Compilation`");
		}
#endif // AVPRO_MOVIECAPTURE_WEBCAMTEXTURE_SUPPORT
	}
}