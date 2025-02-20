using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

/// <summary>
/// Skaliert ein Objekt, entweder über Transform.localScale oder über die Camera FieldOfView
/// // Script sollte auf einem Objekt mit der Standardskalierung von 1,1,1 gelegt werden, ansonsten müsste der zoomFaktor angepasst werden
/// </summary>
public class ZoomObjectHandler : MonoBehaviour
{
	public float minFieldOfView = 10;
	public float maxFieldOfView = 100;

	[Range(1,50)]
	public float zoomFaktor = 10f;
	public float maxScale = 5;
	public float minScale = 0.3f;
	public float startScale = 1f;

	public enum ZoomType
	{
		ZoomScale,
		ZoomCamera
	}
	public ZoomType zoomType;
	public Camera CustomCamera;

	Transform MyTransform;
	float oldDist = -1;

	IEnumerator Start(){
		MyTransform = this.transform;
		yield return new WaitForEndOfFrame ();

		if (CustomCamera == null)
			CustomCamera = Camera.main;
	}

	void Update()
	{
		zoom ();
	}

	// Wir berechnen den Abstand zwischen beiden Fingern auf dem Touchscreen. Die Different vom aktuellen Abstand und dem Abstand vor einem Frame wird als Maß für die Skalierung verwendet
	void zoom()
	{
		if (Input.touchCount == 2) {
			Touch touch1 = Input.GetTouch (0);
			Touch touch2 = Input.GetTouch (1);

			if (touch1.phase == TouchPhase.Moved && touch2.phase == TouchPhase.Moved) {

				// Umrechnung für unabhängige Screengrößen und Auflösungen
				Vector2 t1 = new Vector2( ( touch1.position.x / Screen.width ), ( touch1.position.y / Screen.height ) );
				Vector2 t2 = new Vector2( ( touch2.position.x / Screen.width ), ( touch2.position.y / Screen.height ) );

				if ( oldDist < 0 ) {
					oldDist = Vector2.Distance ( t1, t2 );
				}
				float curDist = Vector2.Distance ( t1, t2 );
				float scaleFaktor = Mathf.Abs ( oldDist - curDist ) * zoomFaktor;

				if ( curDist > oldDist) {

					switch (zoomType) {
					case ZoomType.ZoomScale:
						MyTransform.localScale += new Vector3 (scaleFaktor, scaleFaktor, scaleFaktor);
						if (MyTransform.localScale.x > maxScale) {
							MyTransform.localScale = new Vector3(maxScale, maxScale, maxScale);
						}
						break;
					case ZoomType.ZoomCamera:
						zoomCamera (-scaleFaktor);
						break;
					default:
						break;
					}
				} else if( curDist < oldDist ) {

					switch (zoomType) {
					case ZoomType.ZoomScale:
						
						MyTransform.localScale += new Vector3 (-scaleFaktor, -scaleFaktor, -scaleFaktor);
						if (MyTransform.localScale.x < minScale) {
							MyTransform.localScale = new Vector3 (minScale,minScale, minScale);
						}
							
						break;
					case ZoomType.ZoomCamera:
						zoomCamera (scaleFaktor);
						break;
					default:
						break;
					}
				}

				oldDist = curDist;
			}   
		} else {
			oldDist = -1;
		}
	}


	void zoomCamera( float faktor ){
		// If the camera is orthographic...
		if (CustomCamera.orthographic)
		{
			// ... change the orthographic size based on the change in distance between the touches.
			CustomCamera.orthographicSize += faktor*100;

			// Make sure the orthographic size never drops below zero.
			CustomCamera.orthographicSize = Mathf.Max(GetComponent<Camera>().orthographicSize, 0.1f);
		}
		else
		{
			// Otherwise change the field of view based on the change in distance between the touches.
			CustomCamera.fieldOfView += faktor*100;

			// Clamp the field of view to make sure it's between 0 and 180.
			CustomCamera.fieldOfView = Mathf.Clamp(CustomCamera.fieldOfView, minFieldOfView, maxFieldOfView);
		}
	}

	public void reset(){
		if(MyTransform != null)
			MyTransform.localScale = new Vector3(startScale,startScale,startScale);
	}
	
}
 