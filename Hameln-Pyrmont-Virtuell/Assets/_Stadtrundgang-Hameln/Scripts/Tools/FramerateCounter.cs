using UnityEngine;
using System.Collections;
using UnityEngine.UI;

// An FPS counter.
// It calculates frames/second over each updateInterval,
// so the display does not keep changing wildly.
public class FramerateCounter : MonoBehaviour {

	public bool showFrameRate = true;
	public Text ownUnityText;
	public enum TextPosition {TopLeft, TopRight, BottomLeft, BottomRight, TopCenter, BottomCenter};
	public TextPosition textPosition;

	[Range(0.1f,1.0f)]
	public float size = 0.4f;
	public Color textColor = Color.white;
	[Range(0,5)]
	public int decimalCount = 0;

	public float updateInterval = 1F;
	public bool hideOnClick = true;

	bool useOnGUI = true;
	private double lastInterval;
	private int frames = 0;
	private float fps;

	GUIStyle myStyle = new GUIStyle();
	Vector2 fontSize;
	float border = 10;
	float borderRight = 10;
	float borderCenter = 60;

	void Start()
	{
		lastInterval = Time.realtimeSinceStartup;
		frames = 0;

		if (!showFrameRate)
			useOnGUI = false;

		if (ownUnityText)
			useOnGUI = false;

		myStyle.fontSize = 6 + (int)(size * Screen.width * 0.05f);

		if (ownUnityText){
			ownUnityText.color = textColor;
		}else {
			myStyle.normal.textColor = textColor;
		}

		fontSize = myStyle.CalcSize( new GUIContent( "60.00" ) );

		if (textPosition == TextPosition.TopRight || textPosition == TextPosition.BottomRight) {
			myStyle.alignment = TextAnchor.UpperRight;
		}else if(textPosition == TextPosition.TopCenter || textPosition == TextPosition.BottomCenter){
			myStyle.alignment = TextAnchor.UpperCenter;
		}
	}

	void OnGUI() 
	{
		if (useOnGUI) 
		{
			if(textPosition == TextPosition.TopLeft)
				GUI.Label (new Rect (0 + border, 0 + border, fontSize.x, fontSize.y), "" + fps.ToString ("f" + decimalCount.ToString()), myStyle);

			else if(textPosition == TextPosition.TopRight)
				GUI.Label (new Rect (Screen.width - fontSize.x - borderRight, 0 + border, fontSize.x, fontSize.y), "" + fps.ToString ("f" + decimalCount.ToString()), myStyle);

			else if(textPosition == TextPosition.BottomLeft)
				GUI.Label (new Rect (0 + border, Screen.height - fontSize.y - border, fontSize.x, fontSize.y), "" + fps.ToString ("f" + decimalCount.ToString()), myStyle);

			else if(textPosition == TextPosition.BottomRight)
				GUI.Label (new Rect (Screen.width - fontSize.x - borderRight, Screen.height-fontSize.y - border, fontSize.x, fontSize.y), "" + fps.ToString ("f" + decimalCount.ToString()), myStyle);
			
			else if(textPosition == TextPosition.TopCenter)
				GUI.Label (new Rect (Screen.width/2 - fontSize.x + (borderCenter*size), 0 + border, fontSize.x, fontSize.y), "" + fps.ToString ("f" + decimalCount.ToString()), myStyle);

			else if(textPosition == TextPosition.BottomCenter)
				GUI.Label (new Rect (Screen.width/2 - fontSize.x + (borderCenter*size), Screen.height - fontSize.y - border, fontSize.x, fontSize.y), "" + fps.ToString ("f" + decimalCount.ToString()), myStyle);

		}

	}

	void Update() 
	{
		if (showFrameRate) 
		{
			++frames;
			float timeNow = Time.realtimeSinceStartup;
			if (timeNow > lastInterval + updateInterval) {
				fps = (float)(frames / (timeNow - lastInterval));
				frames = 0;
				lastInterval = timeNow;

				if (ownUnityText != null)
					ownUnityText.text = "" + fps.ToString ("f2");
			}
		}

		if (Input.GetMouseButtonDown (0) && hideOnClick) {

			if (!ownUnityText) {
				Vector3 m = Input.mousePosition;
				if (textPosition == TextPosition.TopLeft) {
					if (m.x < border + fontSize.x && m.y > Screen.height - (border + fontSize.y)) {
						useOnGUI = !useOnGUI;
					}
				} else if (textPosition == TextPosition.TopRight) {
					if (m.x > Screen.width - (border + fontSize.x) && m.y > Screen.height - (border + fontSize.y)) {
						useOnGUI = !useOnGUI;
					}
				} else if (textPosition == TextPosition.BottomLeft) {
					if (m.x < border + fontSize.x && m.y < border + fontSize.y) {
						useOnGUI = !useOnGUI;
					}
				} else if (textPosition == TextPosition.BottomRight) {
					if (m.x > Screen.width - (border + fontSize.x) && m.y < border + fontSize.y) {
						useOnGUI = !useOnGUI;
					}
				} else if (textPosition == TextPosition.TopCenter) {
					if (m.x > Screen.width / 2 - (border + fontSize.x) && m.x < Screen.width / 2 + (border + fontSize.x) && m.y > Screen.height - (border + fontSize.y)) {
						useOnGUI = !useOnGUI;
					}
				} else if (textPosition == TextPosition.BottomCenter) {
					if (m.x > Screen.width / 2 - (border + fontSize.x) && m.x < Screen.width / 2 + (border + fontSize.x) && m.y < border + fontSize.y) {
						useOnGUI = !useOnGUI;
					}
				}
			}
		}

	}
}