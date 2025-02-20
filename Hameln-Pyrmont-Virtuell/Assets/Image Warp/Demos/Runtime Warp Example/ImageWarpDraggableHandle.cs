using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Fenderrio.ImageWarp;

public class ImageWarpDraggableHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
	public ImageWarpHandleType m_handleToAffect;

	[SerializeField]
	private ImageWarp m_imageWarp;

	public void OnBeginDrag(PointerEventData eventData)
	{
		SetDraggedPosition(eventData);
	}

	public void OnDrag(PointerEventData data)
	{
		SetDraggedPosition(data);
	}

	public void OnEndDrag(PointerEventData data)
	{
		SetDraggedPosition(data);
	}

	public System.Action<ImageWarpHandleType, Vector3> OnDragPositionEvent = null;

	private void SetDraggedPosition(PointerEventData data)
	{
		if (data.pointerEnter != null && data.pointerEnter.transform as RectTransform != null)
		{
			RectTransform m_DraggingPlane = data.pointerEnter.transform as RectTransform;

			Vector3 globalMousePos = Vector3.zero;
			if (RectTransformUtility.ScreenPointToWorldPointInRectangle (m_DraggingPlane, data.position, data.pressEventCamera, out globalMousePos))
			{
				transform.position = globalMousePos;

				if (OnDragPositionEvent != null)
				{
					OnDragPositionEvent ( m_handleToAffect, globalMousePos );
				}

				if (m_imageWarp != null)
				{
					m_imageWarp.SetHandleWorldPosition (m_handleToAffect, globalMousePos);
				}
			}
		}
	}


	private void OnValidate()
	{
		m_imageWarp = GetComponentInParent<ImageWarp> ();

		if (m_imageWarp != null)
		{
			transform.position = m_imageWarp.GetHandleWorldPosition (m_handleToAffect);
		}
	}
}
