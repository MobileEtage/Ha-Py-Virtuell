using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CameraNavigationController : MonoBehaviour {
	
	public bool disableScreenSleep = false;
	
	public GameObject cameraRoot;
	public Camera singleCamera;
	public Camera leftCamera;
	public Camera rightCamera;
	
	public enum NavigationTypeMobile
	{
		Disabled,
		Gyro,
		MouseDrag
	}
	
	public enum NavigationType
	{
		Disabled,
		MouseDrag,
		MouseMove
	}
	
	public NavigationType navigationEditor;
	public NavigationType navigationDesktop;
	public NavigationTypeMobile navigationAndroid;
	public NavigationTypeMobile navigationIOS;
	
	public enum GyroType
	{		
		Cardboard,
		MagicWindow,
		UnityGyro,
		GoogleGyro
	}
	
	public GyroType gyroTypeAndroid;
	public GyroType gyroTypeIOS;
	
	public enum CameraMode
	{		
		Mono,
		Stereo
	}
	
	public CameraMode cameraMode;
	
	
	/*************** MouseMouse parameters ****************/
	public enum RotationAxes { MouseXAndY = 0, MouseX = 1, MouseY = 2 }
	public RotationAxes axes = RotationAxes.MouseXAndY;
	public float sensitivityX = 15F;
	public float sensitivityY = 15F;
	public float minimumX = -360F;
	public float maximumX = 360F;
	public float minimumY = -60F;
	public float maximumY = 60F;
	public float speed = 1f;				
	public bool useMarginMovement = true;
	public float marginSpeed = 8f;
	public float marginXPercentage = 0.3f;
	public float marginYPercentage = 0.1f;
	public bool isActive = true;
	
	Quaternion originalRotation;

	float rotationX = 0F;
	float rotationY = 0F;
	float currentMouseX = 0;
	float currentMouseY = 0;

	/*************** MouseDrag parameters ****************/
	public Vector3 rotationOffset = Vector3.zero;
	Vector3 FirstPoint;
	Vector3 SecondPoint;
	float xAngle = 0;
	float yAngle = 0;
	float xAngleTemp = 0;
	float yAngleTemp = 0;
	
	/*************** GoogleGyro parameters ****************/
	// Optional, allows user to drag left/right to rotate the world.
	public bool enableDrag = false;
	public float DRAG_RATE = .2f;
	float dragYawDegrees;
	private bool drag = false;
	
	void Start () {
		
		//if( disableScreenSleep ) Screen.sleepTimeout = SleepTimeout.NeverSleep;
		
		if( cameraRoot == null ){
			cameraRoot = this.gameObject;
		}
		
		#if UNITY_EDITOR
		
		if( navigationEditor == NavigationType.MouseDrag ){
			InitMouseDragNavigation();
		}else if( navigationEditor == NavigationType.MouseMove ){
			InitMouseMoveNavigation();
		}
		
		#elif UNITY_ANDROID
		
		if( navigationAndroid == NavigationTypeMobile.Gyro ){
				
		if( gyroTypeAndroid == GyroType.Cardboard ){
		InitCardboardNavigation();
		}else if( gyroTypeAndroid == GyroType.MagicWindow ){
		InitMagicWindowNavigation();
		}else{
		Input.gyro.enabled = true;
		}
		
		}else if( navigationAndroid == NavigationTypeMobile.MouseDrag ){
		InitMouseDragNavigation();
		}
		
		#elif UNITY_IOS
		
		if( navigationIOS == NavigationTypeMobile.Gyro ){
			
		if( gyroTypeIOS == GyroType.Cardboard ){
		InitCardboardNavigation();
		}else if( gyroTypeIOS == GyroType.MagicWindow ){
		InitMagicWindowNavigation();
		}else{
		Input.gyro.enabled = true;
		}
		
		}else if( navigationIOS == NavigationTypeMobile.MouseDrag ){
		InitMouseDragNavigation();
		}
		
		#elif UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_WEBGL
		
		if( navigationDesktop == NavigationType.MouseDrag ){
		InitMouseDragNavigation();
		}else if( navigationDesktop == NavigationType.MouseMove ){
		InitMouseMoveNavigation();
		}
		
		#endif
		
	}
	
	void Update(){
		
		if( !isActive ) return;
		
		#if UNITY_EDITOR
		HandleEditorNavigation();
		#elif UNITY_ANDROID
		HandleAndroidNavigation();
		#elif UNITY_IOS
		HandleIOSNavigation();
		#elif UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_WEBGL
		HandleDesktopNavigation();
		#endif		
	}
	
	
	
	private void HandleEditorNavigation(){
		if( navigationEditor == NavigationType.MouseDrag ){
			HandleMouseDragNavigation();
		}else if( navigationEditor == NavigationType.MouseMove ){
			HandleMouseMoveNavigation();
		}
	}
	
	private void HandleDesktopNavigation(){
		if( navigationDesktop == NavigationType.MouseDrag ){
			HandleMouseDragNavigation();
		}else if( navigationDesktop == NavigationType.MouseMove ){
			HandleMouseMoveNavigation();
		}
	}
	
	private void HandleAndroidNavigation(){
		if( navigationAndroid == NavigationTypeMobile.MouseDrag ){
			HandleMouseDragNavigation();
		}else if( navigationAndroid == NavigationTypeMobile.Gyro ){
			
			if( gyroTypeAndroid == GyroType.UnityGyro ){
				HandleUnityGyroNavigation();
			}else if( gyroTypeAndroid == GyroType.GoogleGyro ){
				HandleGoogleGyroNavigation();
			}else if( gyroTypeAndroid == GyroType.Cardboard ){
				// Rotation will be updated automatically
			}else if( gyroTypeAndroid == GyroType.MagicWindow ){
				HandleMagicModeNavigation();
			}
		}
	}
	
	private void HandleIOSNavigation(){
		if( navigationIOS == NavigationTypeMobile.MouseDrag ){
			HandleMouseDragNavigation();
		}else if( navigationIOS == NavigationTypeMobile.Gyro ){
			
			if( gyroTypeIOS == GyroType.UnityGyro ){
				HandleUnityGyroNavigation();
			}else if( gyroTypeIOS == GyroType.GoogleGyro ){
				HandleGoogleGyroNavigation();
			}else if( gyroTypeIOS == GyroType.Cardboard ){
				// Rotation will be updated automatically
			}else if( gyroTypeIOS == GyroType.MagicWindow ){
				HandleMagicModeNavigation();
			}
		}
	}
	
	/*************** UnityGyro functions ****************/
	
	// The Gyroscope is right-handed.  Unity is left handed.
	// Make the necessary change to the camera.
	private void HandleUnityGyroNavigation()
	{
		cameraRoot.transform.rotation = GyroToUnity(Input.gyro.attitude);
	}

	private static Quaternion GyroToUnity(Quaternion q)
	{
		return new Quaternion(q.x, q.y, -q.z, -q.w);
	}

	/*************** MouseMove functions ****************/
	
	private void InitMouseMoveNavigation(){
		originalRotation = transform.localRotation;
	}
	
	private void HandleMouseMoveNavigation ()
	{

		if (axes == RotationAxes.MouseXAndY)
		{
			if (Input.mousePosition.x < Screen.width * marginXPercentage && useMarginMovement) {
				rotationX -= sensitivityX * Time.deltaTime*speed*marginSpeed;

				rotationX = ClampAngle (rotationX, minimumX, maximumX);
				Quaternion xQuaternion = Quaternion.AngleAxis (rotationX, Vector3.up);
				Quaternion yQuaternion = Quaternion.AngleAxis (rotationY, -Vector3.right);
				transform.localRotation = originalRotation * xQuaternion * yQuaternion;
			} else if (Input.mousePosition.x > Screen.width * (1-marginXPercentage) && useMarginMovement) {
				rotationX += sensitivityX * Time.deltaTime*speed*marginSpeed;

				rotationX = ClampAngle (rotationX, minimumX, maximumX);
				Quaternion xQuaternion = Quaternion.AngleAxis (rotationX, Vector3.up);
				Quaternion yQuaternion = Quaternion.AngleAxis (rotationY, -Vector3.right);
				transform.localRotation = originalRotation * xQuaternion * yQuaternion;
			} 
			else {

				rotationX -= (currentMouseX-Input.mousePosition.x) * sensitivityX * Time.deltaTime*speed;

				rotationX = ClampAngle (rotationX, minimumX, maximumX);
				Quaternion xQuaternion = Quaternion.AngleAxis (rotationX, Vector3.up);
				Quaternion yQuaternion = Quaternion.AngleAxis (rotationY, -Vector3.right);
				transform.localRotation = originalRotation * xQuaternion * yQuaternion;
			}


			if (Input.mousePosition.y < Screen.height * marginYPercentage && useMarginMovement) {
				rotationY -= sensitivityY * Time.deltaTime*speed*marginSpeed;

				rotationY = ClampAngle (rotationY, minimumY, maximumY);
				Quaternion xQuaternion = Quaternion.AngleAxis (rotationX, Vector3.up);
				Quaternion yQuaternion = Quaternion.AngleAxis (rotationY, -Vector3.right);
				transform.localRotation = originalRotation * xQuaternion * yQuaternion;
			} else if (Input.mousePosition.y > Screen.height * (1-marginYPercentage) && useMarginMovement) {
				rotationY += sensitivityY * Time.deltaTime*speed*marginSpeed;

				rotationY = ClampAngle (rotationY, minimumY, maximumY);
				Quaternion xQuaternion = Quaternion.AngleAxis (rotationX, Vector3.up);
				Quaternion yQuaternion = Quaternion.AngleAxis (rotationY, -Vector3.right);
				transform.localRotation = originalRotation * xQuaternion * yQuaternion;
			} 
			else{

				rotationY -= (currentMouseY-Input.mousePosition.y) * sensitivityY * Time.deltaTime*speed;

				rotationY = ClampAngle (rotationY, minimumY, maximumY);
				Quaternion xQuaternion = Quaternion.AngleAxis (rotationX, Vector3.up);
				Quaternion yQuaternion = Quaternion.AngleAxis (rotationY, -Vector3.right);
				transform.localRotation = originalRotation * xQuaternion * yQuaternion;
			}


			currentMouseX = Input.mousePosition.x;
			currentMouseY = Input.mousePosition.y;
		}
		else if (axes == RotationAxes.MouseX)
		{
			rotationX += Input.GetAxis("Mouse X") * sensitivityX;
			rotationX = ClampAngle (rotationX, minimumX, maximumX);
			Quaternion xQuaternion = Quaternion.AngleAxis (rotationX, Vector3.up);
			transform.localRotation = originalRotation * xQuaternion;
		}
		else
		{
			rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
			rotationY = ClampAngle (rotationY, minimumY, maximumY);
			Quaternion yQuaternion = Quaternion.AngleAxis (-rotationY, Vector3.right);
			transform.localRotation = originalRotation * yQuaternion;
		}
	}
	
	public float ClampAngle (float angle, float min, float max)
	{
		if (angle < -360F)
			angle += 360F;
		if (angle > 360F)
			angle -= 360F;
		return Mathf.Clamp (angle, min, max);
	}
	
	
	/*************** MouseDrag functions ****************/

	public void InitMouseDragNavigation(){
		
		xAngle = cameraRoot.transform.localEulerAngles.x;
		yAngle = cameraRoot.transform.localEulerAngles.y;
		cameraRoot.transform.localEulerAngles =new Vector3( xAngle, yAngle, 0) - rotationOffset;
	}
	
	private void HandleMouseDragNavigation(){
		
		/*
		if( EventSystem.current != null && !drag ){
			if( EventSystem.current.IsPointerOverGameObject() ){
				return;
			}
		}
		*/
		
		if(Input.GetMouseButtonDown(1)){

			FirstPoint = Input.mousePosition;
			//xAngleTemp = xAngle;
			//yAngleTemp = yAngle;
			xAngleTemp = cameraRoot.transform.localEulerAngles.x;
			yAngleTemp = cameraRoot.transform.localEulerAngles.y;	
			
			drag = true;
		}
		if (Input.GetMouseButton(1) && drag)
		{			
			SecondPoint = Input.mousePosition;
			xAngle = xAngleTemp + (SecondPoint.y - FirstPoint.y) * 90 / Screen.height;
			yAngle = yAngleTemp - (SecondPoint.x - FirstPoint.x) * 180 / Screen.width;

			if( (xAngle > -360 && xAngle < -300) ||
				(xAngle > 300 && xAngle < 360) 
			){
				
			}else{
				xAngle = ClampAngle(xAngle, -60, 60);				
			}
			yAngle = ClampAngle(yAngle, -360, 360);
			
			cameraRoot.transform.localEulerAngles = new Vector3( xAngle, yAngle, 0.0f) - rotationOffset;			
		}
		
		if (Input.GetMouseButtonUp(1))
		{			
			drag = false;			
		}
	}
	
	public void UpdateRotation( float xDelta, float yDelta ){
		
		xAngle = ClampAngle(xAngle+xDelta, -60, 60);
		yAngle = ClampAngle(yAngle+yDelta, -360, 360);
		
		cameraRoot.transform.localEulerAngles = new Vector3( xAngle, yAngle, 0.0f) - rotationOffset;
	}
	
	/*************** GoogleGyro functions ****************/
	
	public void HandleGoogleGyroNavigation(){
		
		if (XRSettings.enabled) {
			// Unity takes care of updating camera transform in VR.
			return;
		}

		// android-developers.blogspot.com/2010/09/one-screen-turn-deserves-another.html
		// developer.android.com/guide/topics/sensors/sensors_overview.html#sensors-coords
		//
		//     y                                       x
		//     |  Gyro upright phone                   |  Gyro landscape left phone
		//     |                                       |
		//     |______ x                      y  ______|
		//     /                                       \
		//    /                                         \
		//   z                                           z
		//
		//
		//     y
		//     |  z   Unity
		//     | /
		//     |/_____ x
		//

		// Update `dragYawDegrees` based on user touch.
		if( enableDrag ) CheckDrag ();

		transform.localRotation =
			// Allow user to drag left/right to adjust direction they're facing.
			Quaternion.Euler (0f, -dragYawDegrees, 0f) *

			// Neutral position is phone held upright, not flat on a table.
			Quaternion.Euler (90f, 0f, 0f) *

			// Sensor reading, assuming default `Input.compensateSensors == true`.
			Input.gyro.attitude *

			// So image is not upside down.
			Quaternion.Euler (0f, 0f, 180f);
	}
	
	void CheckDrag () {
		if (Input.touchCount != 1) {
			return;
		}

		Touch touch = Input.GetTouch (0);
		if (touch.phase != TouchPhase.Moved) {
			return;
		}

		dragYawDegrees += touch.deltaPosition.x * DRAG_RATE;
	}
	
	/*************** Cardboard functions ****************/

	private void InitCardboardNavigation(){
		StartCoroutine (SetModeCardboardRoutine ());
	}
	
	// Change to google cardboard stereo rendering
	private IEnumerator SetModeCardboardRoutine()
	{
		UnityEngine.XR.XRSettings.LoadDeviceByName("cardboard");
		yield return new WaitForSeconds(0.2f);
		UnityEngine.XR.XRSettings.enabled = true;
		yield return new WaitForSeconds(0.2f);
		ResetCameraAspect();
	}

	/*************** MagicWindow functions ****************/

	private void InitMagicWindowNavigation(){
		StartCoroutine (SetModeMagicWindowRoutine ());
	}
	
	// Disable VRSettings, but keep loaded device "cardboard" --> with "XRInput.GetLocalRotation (VRNode.CenterEye);" we get still the gyroscope rotation
	private IEnumerator SetModeMagicWindowRoutine()
	{
		UnityEngine.XR.XRSettings.LoadDeviceByName("cardboard");
		// Delay as LoadDeviceByName "Loads the requested device at the beginning of the next frame."
		yield return new WaitForSeconds(0.2f);
		UnityEngine.XR.XRSettings.enabled = false;
		yield return new WaitForSeconds(0.2f);
		ResetCameraAspect();
	}
	
	private void HandleMagicModeNavigation(){
		if (!UnityEngine.XR.XRSettings.enabled) {
			cameraRoot.transform.localRotation = UnityEngine.XR.InputTracking.GetLocalRotation (UnityEngine.XR.XRNode.CenterEye);
		}
	}
	
	private void HandleWideScreen(){

		//float defaultRatio = 16.0f / 9.0f;
		float maxRatio = 17.0f / 9.0f;
		float screenRatio = (float)Screen.width / (float)Screen.height;

		float difRatio = screenRatio - maxRatio;

		if(difRatio > 0){
			float percentage = difRatio / screenRatio;
			float cameraOffsetRect = Mathf.Clamp( 0.5f * percentage, 0, 0.1f );

			print(cameraOffsetRect);

			leftCamera.rect = new Rect(cameraOffsetRect, 0, 0.5f - cameraOffsetRect, 1);
			rightCamera.rect = new Rect(0.5f, 0, 0.5f - cameraOffsetRect, 1);
		}

	}
	
	// We do ResetAspect as with Unity version 2017.1.b8 when disabling VR the camera aspect is distorted.
	private void ResetCameraAspect(){
		if (singleCamera != null) {
			singleCamera.ResetAspect();
		}
		if (leftCamera != null) {
			leftCamera.ResetAspect();
		}
		if (rightCamera != null) {
			rightCamera.ResetAspect();
		}
	}

}



#if UNITY_EDITOR

[CustomEditor(typeof(CameraNavigationController), true)]
public class CameraNavigationControllerEditor : Editor
{
	public override void OnInspectorGUI()
	{
		CameraNavigationController myTarget = (CameraNavigationController)target;
		
		EditorGUILayout.Space();
		EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

		EditorGUILayout.LabelField("Option to disable screen sleep", EditorStyles.boldLabel);
		EditorGUILayout.PropertyField(serializedObject.FindProperty("disableScreenSleep"));
		
		EditorGUILayout.Space();

		EditorGUILayout.LabelField("The main transform to rotate", EditorStyles.boldLabel);
		EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraRoot"));
		
		EditorGUILayout.Space();
		
		EditorGUILayout.LabelField("Use a single camera or two cameras", EditorStyles.boldLabel);
		EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraMode"));
		if (myTarget.cameraMode == CameraNavigationController.CameraMode.Mono)
		{
			EditorGUILayout.PropertyField(serializedObject.FindProperty("singleCamera"));
		}
		else if (myTarget.cameraMode == CameraNavigationController.CameraMode.Stereo)
		{
			EditorGUILayout.PropertyField(serializedObject.FindProperty("leftCamera"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("rightCamera"));
		}
		
		EditorGUILayout.Space();
		EditorGUILayout.Space();
		EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
		EditorGUILayout.Space();
		
		EditorGUILayout.PropertyField(serializedObject.FindProperty("navigationEditor"));
		
		EditorGUILayout.Space();
		
		#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_WEBGL
		EditorGUILayout.PropertyField(serializedObject.FindProperty("navigationDesktop"));
		EditorGUILayout.Space();
		#else
		myTarget.navigationDesktop = CameraNavigationController.NavigationType.Disabled;
		#endif
		
		#if UNITY_ANDROID
		EditorGUILayout.PropertyField(serializedObject.FindProperty("navigationAndroid"));
		if ( myTarget.navigationAndroid == CameraNavigationController.NavigationTypeMobile.Gyro )
		{			
			EditorGUILayout.PropertyField(serializedObject.FindProperty("gyroTypeAndroid"));
			if (myTarget.gyroTypeAndroid == CameraNavigationController.GyroType.GoogleGyro )
			{
				EditorGUILayout.PropertyField(serializedObject.FindProperty("enableDrag"));
				if( myTarget.enableDrag ){
					EditorGUILayout.PropertyField(serializedObject.FindProperty("DRAG_RATE"));
				}
			}
		}
		#else
		myTarget.navigationAndroid = CameraNavigationController.NavigationTypeMobile.Disabled;
		#endif
		
		EditorGUILayout.Space();
		
		#if UNITY_IOS
		EditorGUILayout.PropertyField(serializedObject.FindProperty("navigationIOS"));
		if ( myTarget.navigationIOS == CameraNavigationController.NavigationTypeMobile.Gyro )
		{			
		EditorGUILayout.PropertyField(serializedObject.FindProperty("gyroTypeIOS"));
		if (myTarget.gyroTypeIOS == CameraNavigationController.GyroType.GoogleGyro )
		{
		EditorGUILayout.PropertyField(serializedObject.FindProperty("enableDrag"));
		if( myTarget.enableDrag ){
		EditorGUILayout.PropertyField(serializedObject.FindProperty("DRAG_RATE"));
		}
		}
		}
		#else
		myTarget.navigationIOS = CameraNavigationController.NavigationTypeMobile.Disabled;
		#endif
		
		if (myTarget.navigationEditor == CameraNavigationController.NavigationType.MouseDrag 
			|| myTarget.navigationDesktop == CameraNavigationController.NavigationType.MouseDrag 
			|| myTarget.navigationAndroid == CameraNavigationController.NavigationTypeMobile.MouseDrag
			|| myTarget.navigationIOS == CameraNavigationController.NavigationTypeMobile.MouseDrag
		)
		{
			EditorGUILayout.Space();
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Mouse drag settings", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("rotationOffset"));
		}
		
		if (myTarget.navigationEditor == CameraNavigationController.NavigationType.MouseMove 
			|| myTarget.navigationDesktop == CameraNavigationController.NavigationType.MouseMove
		)
		{
			EditorGUILayout.Space();
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Mouse move settings", EditorStyles.boldLabel);

			EditorGUILayout.PropertyField(serializedObject.FindProperty("axes"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("sensitivityX"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("sensitivityY"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("minimumX"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("maximumX"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("minimumY"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("maximumY"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("speed"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("useMarginMovement"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("marginSpeed"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("marginXPercentage"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("marginYPercentage"));
		}

		
		serializedObject.ApplyModifiedProperties();
	}
}

#endif


