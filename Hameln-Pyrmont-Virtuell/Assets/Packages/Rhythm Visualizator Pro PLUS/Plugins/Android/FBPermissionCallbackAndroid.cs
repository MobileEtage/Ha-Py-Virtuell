﻿using System.Threading;
using UnityEngine;


	public class FBPermissionCallbackAndroid
#if UNITY_ANDROID
	: AndroidJavaProxy
	{
		private object threadLock;
		public int Result { get; private set; }

		public FBPermissionCallbackAndroid( object threadLock ) : base( "com.rhythmvisualizatorpro.unity.FileBrowserPermissionReceiver" )
		{
			Result = -1;
			this.threadLock = threadLock;
		}

		public void OnPermissionResult( int result )
		{
			Result = result;

			lock( threadLock )
			{
				Monitor.Pulse( threadLock );
			}
		}
	}
#else
	{ }
#endif
