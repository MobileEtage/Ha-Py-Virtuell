using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Fenderrio.ImageWarp
{
	[AddComponentMenu("UI/Raw Image Warp")]
	public class RawImageWarp : RawImage, IWarp
	{
		[SerializeField] private bool m_flipX;
		public bool flipX { get { return m_flipX; } set { if(SetStruct(ref m_flipX, value)) { SetVerticesDirty (); } } }

		[SerializeField] private bool m_flipY;
		public bool flipY { get { return m_flipY; } set { if(SetStruct(ref m_flipY, value)) { SetVerticesDirty (); } } }

		[SerializeField] private Vector3 m_cornerOffsetTL;
		public Vector3 cornerOffsetTL { get { return m_cornerOffsetTL; } set { if (SetStruct(ref m_cornerOffsetTL, value)) SetVerticesDirty(); } }

		[SerializeField] private Vector3 m_cornerOffsetTR;
		public Vector3 cornerOffsetTR { get { return m_cornerOffsetTR; } set { if (SetStruct(ref m_cornerOffsetTR, value)) SetVerticesDirty(); } }

		[SerializeField] private Vector3 m_cornerOffsetBR;
		public Vector3 cornerOffsetBR { get { return m_cornerOffsetBR; } set { if (SetStruct(ref m_cornerOffsetBR, value)) SetVerticesDirty(); } }

		[SerializeField] private Vector3 m_cornerOffsetBL;
		public Vector3 cornerOffsetBL { get { return m_cornerOffsetBL; } set { if (SetStruct(ref m_cornerOffsetBL, value)) SetVerticesDirty(); } }

		[SerializeField] private int m_numSubdivisions = 10;
		public int numSubdivisions { get { return m_numSubdivisions; } set { if (SetStruct (ref m_numSubdivisions, value)) SetVerticesDirty (); } }

		[SerializeField] private bool m_bezierEdges = false;
		public bool bezierEdges { get { return m_bezierEdges; } set { if (SetStruct (ref m_bezierEdges, value)) SetVerticesDirty (); } }

		[SerializeField] private Vector3 m_topBezierHandleA;
		public Vector3 topBezierHandleA { get { return m_topBezierHandleA; } set { if (SetStruct (ref m_topBezierHandleA, value)) SetVerticesDirty (); } }

		[SerializeField] private Vector3 m_topBezierHandleB;
		public Vector3 topBezierHandleB { get { return m_topBezierHandleB; } set { if (SetStruct (ref m_topBezierHandleB, value)) SetVerticesDirty (); } }

		[SerializeField] private Vector3 m_leftBezierHandleA;
		public Vector3 leftBezierHandleA { get { return m_leftBezierHandleA; } set { if (SetStruct (ref m_leftBezierHandleA, value)) SetVerticesDirty (); } }

		[SerializeField] private Vector3 m_leftBezierHandleB;
		public Vector3 leftBezierHandleB { get { return m_leftBezierHandleB; } set { if (SetStruct (ref m_leftBezierHandleB, value)) SetVerticesDirty (); } }

		[SerializeField] private Vector3 m_rightBezierHandleA;
		public Vector3 rightBezierHandleA { get { return m_rightBezierHandleA; } set { if (SetStruct (ref m_rightBezierHandleA, value)) SetVerticesDirty (); } }

		[SerializeField] private Vector3 m_rightBezierHandleB;
		public Vector3 rightBezierHandleB { get { return m_rightBezierHandleB; } set { if (SetStruct (ref m_rightBezierHandleB, value)) SetVerticesDirty (); } }

		[SerializeField] private Vector3 m_bottomBezierHandleA;
		public Vector3 bottomBezierHandleA { get { return m_bottomBezierHandleA; } set { if (SetStruct (ref m_bottomBezierHandleA, value)) SetVerticesDirty (); } }

		[SerializeField] private Vector3 m_bottomBezierHandleB;
		public Vector3 bottomBezierHandleB { get { return m_bottomBezierHandleB; } set { if (SetStruct (ref m_bottomBezierHandleB, value)) SetVerticesDirty (); } }

		[SerializeField] private float m_cropLeft;
		public float cropLeft { get { return m_cropLeft; } set { if (SetStruct(ref m_cropLeft, value)) SetVerticesDirty (); } }

		[SerializeField] private float m_cropRight;
		public float cropRight { get { return m_cropRight; } set { if (SetStruct(ref m_cropRight, value)) SetVerticesDirty (); } }

		[SerializeField] private float m_cropTop;
		public float cropTop { get { return m_cropTop; } set { if (SetStruct(ref m_cropTop, value)) SetVerticesDirty (); } }

		[SerializeField] private float m_cropBottom;
		public float cropBottom { get { return m_cropBottom; } set { if (SetStruct(ref m_cropBottom, value)) SetVerticesDirty (); } }

		public Vector3 cornerWorldPositionTL { get { return transform.TransformPoint( m_warpManager.CornerPositionTL ); } }
		public Vector3 cornerLocalPositionTL { get { return m_warpManager.CornerPositionTL; } }

		public Vector3 cornerWorldPositionTR { get { return transform.TransformPoint( m_warpManager.CornerPositionTR ); } }
		public Vector3 cornerLocalPositionTR { get { return m_warpManager.CornerPositionTR; } }

		public Vector3 cornerWorldPositionBR { get { return transform.TransformPoint( m_warpManager.CornerPositionBR ); } }
		public Vector3 cornerLocalPositionBR { get { return m_warpManager.CornerPositionBR; } }

		public Vector3 cornerWorldPositionBL { get { return transform.TransformPoint( m_warpManager.CornerPositionBL ); } }
		public Vector3 cornerLocalPositionBL { get { return m_warpManager.CornerPositionBL; } }


		public Vector3 topBezierWorldPositionHandleA { get { return transform.TransformPoint( m_warpManager.TopCurve.GetCurvePointHandleA(0) ); } }
		public Vector3 topBezierLocalPositionHandleA { get { return m_warpManager.TopCurve.GetCurvePointHandleA(0); } }
		public Vector3 topBezierWorldPositionHandleB { get { return transform.TransformPoint( m_warpManager.TopCurve.GetCurvePointHandleA(1) ); } }
		public Vector3 topBezierLocalPositionHandleB { get { return m_warpManager.TopCurve.GetCurvePointHandleA(1); } }

		public Vector3 rightBezierWorldPositionHandleA { get { return transform.TransformPoint( m_warpManager.RightCurve.GetCurvePointHandleA(0) ); } }
		public Vector3 rightBezierLocalPositionHandleA { get { return m_warpManager.RightCurve.GetCurvePointHandleA(0); } }
		public Vector3 rightBezierWorldPositionHandleB { get { return transform.TransformPoint( m_warpManager.RightCurve.GetCurvePointHandleA(1) ); } }
		public Vector3 rightBezierLocalPositionHandleB { get { return m_warpManager.RightCurve.GetCurvePointHandleA(1); } }

		public Vector3 bottomBezierWorldPositionHandleA { get { return transform.TransformPoint( m_warpManager.BottomCurve.GetCurvePointHandleA(0) ); } }
		public Vector3 bottomBezierLocalPositionHandleA { get { return m_warpManager.BottomCurve.GetCurvePointHandleA(0); } }
		public Vector3 bottomBezierWorldPositionHandleB { get { return transform.TransformPoint( m_warpManager.BottomCurve.GetCurvePointHandleA(1) ); } }
		public Vector3 bottomBezierLocalPositionHandleB { get { return m_warpManager.BottomCurve.GetCurvePointHandleA(1); } }

		public Vector3 leftBezierWorldPositionHandleA { get { return transform.TransformPoint( m_warpManager.LeftCurve.GetCurvePointHandleA(0) ); } }
		public Vector3 leftBezierLocalPositionHandleA { get { return m_warpManager.LeftCurve.GetCurvePointHandleA(0); } }
		public Vector3 leftBezierWorldPositionHandleB { get { return transform.TransformPoint( m_warpManager.LeftCurve.GetCurvePointHandleA(1) ); } }
		public Vector3 leftBezierLocalPositionHandleB { get { return m_warpManager.LeftCurve.GetCurvePointHandleA(1); } }


		public void SetCornerWorldPositionTL(Vector3 a_worldPosition) { SetCornerLocalPositionTL ( transform.InverseTransformPoint (a_worldPosition) ); }
		public void SetCornerLocalPositionTL(Vector3 a_localPosition)
		{
			cornerOffsetTL = a_localPosition 	- m_cropLeft * m_warpManager.PreWarpMeshXDiff + m_cropTop * m_warpManager.PreWarpMeshYDiff
				- m_warpManager.OriginalCornerPositionTL;
		}

		public void SetCornerWorldPositionTR(Vector3 a_worldPosition) { SetCornerLocalPositionTR ( transform.InverseTransformPoint (a_worldPosition) ); }
		public void SetCornerLocalPositionTR(Vector3 a_localPosition)
		{
			cornerOffsetTR = a_localPosition 	+ m_cropRight * m_warpManager.PreWarpMeshXDiff + m_cropTop * m_warpManager.PreWarpMeshYDiff
				- m_warpManager.OriginalCornerPositionTR;
		}

		public void SetCornerWorldPositionBR(Vector3 a_worldPosition) { SetCornerLocalPositionBR ( transform.InverseTransformPoint (a_worldPosition) ); }
		public void SetCornerLocalPositionBR(Vector3 a_localPosition)
		{
			cornerOffsetBR = a_localPosition 	+ m_cropRight * m_warpManager.PreWarpMeshXDiff - m_cropBottom * m_warpManager.PreWarpMeshYDiff
				- m_warpManager.OriginalCornerPositionBR;
		}

		public void SetCornerWorldPositionBL(Vector3 a_worldPosition) { SetCornerLocalPositionBL ( transform.InverseTransformPoint (a_worldPosition) ); }
		public void SetCornerLocalPositionBL(Vector3 a_localPosition)
		{
			cornerOffsetBL = a_localPosition 	- m_cropLeft * m_warpManager.PreWarpMeshXDiff - m_cropBottom * m_warpManager.PreWarpMeshYDiff
				- m_warpManager.OriginalCornerPositionBL;
		}



		public void SetTopBezierWorldPositionHandleA(Vector3 a_worldPosition) { SetTopBezierLocalPositionHandleA ( transform.InverseTransformPoint (a_worldPosition) ); }
		public void SetTopBezierLocalPositionHandleA(Vector3 a_localPosition)
		{
			topBezierHandleA = a_localPosition 	+ -m_cropLeft * m_warpManager.PreWarpMeshXDiff + m_cropTop * m_warpManager.PreWarpMeshYDiff
				- m_warpManager.OriginalCornerPositionTL - m_cornerOffsetTL;
		}
		public void SetTopBezierWorldPositionHandleB(Vector3 a_worldPosition) { SetTopBezierLocalPositionHandleB ( transform.InverseTransformPoint (a_worldPosition) ); }
		public void SetTopBezierLocalPositionHandleB(Vector3 a_localPosition)
		{
			topBezierHandleB = a_localPosition 	+ m_cropRight * m_warpManager.PreWarpMeshXDiff + m_cropTop * m_warpManager.PreWarpMeshYDiff
				- m_warpManager.OriginalCornerPositionTR - m_cornerOffsetTR;
		}

		public void SetRightBezierWorldPositionHandleA(Vector3 a_worldPosition) { SetRightBezierLocalPositionHandleA ( transform.InverseTransformPoint (a_worldPosition) ); }
		public void SetRightBezierLocalPositionHandleA(Vector3 a_localPosition)
		{
			rightBezierHandleA = a_localPosition 	+ m_cropRight * m_warpManager.PreWarpMeshXDiff + m_cropTop * m_warpManager.PreWarpMeshYDiff
				- m_warpManager.OriginalCornerPositionTR - m_cornerOffsetTR;
		}
		public void SetRightBezierWorldPositionHandleB(Vector3 a_worldPosition) { SetRightBezierLocalPositionHandleB ( transform.InverseTransformPoint (a_worldPosition) ); }
		public void SetRightBezierLocalPositionHandleB(Vector3 a_localPosition)
		{
			rightBezierHandleB = a_localPosition 	+ m_cropRight * m_warpManager.PreWarpMeshXDiff - m_cropBottom * m_warpManager.PreWarpMeshYDiff
				- m_warpManager.OriginalCornerPositionBR - m_cornerOffsetBR;
		}

		public void SetBottomBezierWorldPositionHandleA(Vector3 a_worldPosition) { SetBottomBezierLocalPositionHandleA ( transform.InverseTransformPoint (a_worldPosition) ); }
		public void SetBottomBezierLocalPositionHandleA(Vector3 a_localPosition)
		{
			bottomBezierHandleA = a_localPosition 	+ m_cropRight * m_warpManager.PreWarpMeshXDiff - m_cropBottom * m_warpManager.PreWarpMeshYDiff
				- m_warpManager.OriginalCornerPositionBR - m_cornerOffsetBR;
		}
		public void SetBottomBezierWorldPositionHandleB(Vector3 a_worldPosition) { SetBottomBezierLocalPositionHandleB ( transform.InverseTransformPoint (a_worldPosition) ); }
		public void SetBottomBezierLocalPositionHandleB(Vector3 a_localPosition)
		{
			bottomBezierHandleB = a_localPosition 	- m_cropLeft * m_warpManager.PreWarpMeshXDiff - m_cropBottom * m_warpManager.PreWarpMeshYDiff
				- m_warpManager.OriginalCornerPositionBL - m_cornerOffsetBL;
		}

		public void SetLeftBezierWorldPositionHandleA(Vector3 a_worldPosition) { SetLeftBezierLocalPositionHandleA ( transform.InverseTransformPoint (a_worldPosition) ); }
		public void SetLeftBezierLocalPositionHandleA(Vector3 a_localPosition)
		{
			leftBezierHandleA = a_localPosition 	- m_cropLeft * m_warpManager.PreWarpMeshXDiff - m_cropBottom * m_warpManager.PreWarpMeshYDiff
				- m_warpManager.OriginalCornerPositionBL - m_cornerOffsetBL;
		}
		public void SetLeftBezierWorldPositionHandleB(Vector3 a_worldPosition) { SetLeftBezierLocalPositionHandleB ( transform.InverseTransformPoint (a_worldPosition) ); }
		public void SetLeftBezierLocalPositionHandleB(Vector3 a_localPosition)
		{
			leftBezierHandleB = a_localPosition 	- m_cropLeft * m_warpManager.PreWarpMeshXDiff + m_cropTop * m_warpManager.PreWarpMeshYDiff
				- m_warpManager.OriginalCornerPositionTL - m_cornerOffsetTL;
		}

		public Vector3 GetHandleWorldPosition(ImageWarpHandleType a_handleType)
		{
			switch(a_handleType)
			{
				case ImageWarpHandleType.CornerTL:
					return cornerWorldPositionTL;
				case ImageWarpHandleType.CornerTR:
					return cornerWorldPositionTR;
				case ImageWarpHandleType.CornerBR:
					return cornerWorldPositionBR;
				case ImageWarpHandleType.CornerBL:
					return cornerWorldPositionBL;

				case ImageWarpHandleType.TopBezierHandleA:
					return topBezierWorldPositionHandleA;
				case ImageWarpHandleType.TopBezierHandleB:
					return topBezierWorldPositionHandleB;
				case ImageWarpHandleType.RightBezierHandleA:
					return rightBezierWorldPositionHandleA;
				case ImageWarpHandleType.RightBezierHandleB:
					return rightBezierWorldPositionHandleB;
				case ImageWarpHandleType.BottomBezierHandleA:
					return bottomBezierWorldPositionHandleA;
				case ImageWarpHandleType.BottomBezierHandleB:
					return bottomBezierWorldPositionHandleB;
				case ImageWarpHandleType.LeftBezierHandleA:
					return leftBezierWorldPositionHandleA;
				case ImageWarpHandleType.LeftBezierHandleB:
					return leftBezierWorldPositionHandleB;
			}

			return Vector3.zero;
		}

		public Vector3 GetHandleLocalPosition(ImageWarpHandleType a_handleType)
		{
			switch(a_handleType)
			{
				case ImageWarpHandleType.CornerTL:
					return cornerLocalPositionTL;
				case ImageWarpHandleType.CornerTR:
					return cornerLocalPositionTR;
				case ImageWarpHandleType.CornerBR:
					return cornerLocalPositionBR;
				case ImageWarpHandleType.CornerBL:
					return cornerLocalPositionBL;

				case ImageWarpHandleType.TopBezierHandleA:
					return topBezierLocalPositionHandleA;
				case ImageWarpHandleType.TopBezierHandleB:
					return topBezierLocalPositionHandleB;
				case ImageWarpHandleType.RightBezierHandleA:
					return rightBezierLocalPositionHandleA;
				case ImageWarpHandleType.RightBezierHandleB:
					return rightBezierLocalPositionHandleB;
				case ImageWarpHandleType.BottomBezierHandleA:
					return bottomBezierLocalPositionHandleA;
				case ImageWarpHandleType.BottomBezierHandleB:
					return bottomBezierLocalPositionHandleB;
				case ImageWarpHandleType.LeftBezierHandleA:
					return leftBezierLocalPositionHandleA;
				case ImageWarpHandleType.LeftBezierHandleB:
					return leftBezierLocalPositionHandleB;
			}

			return Vector3.zero;
		}

		public void SetHandleWorldPosition(ImageWarpHandleType a_handleType, Vector3 a_worldPosition)
		{
			switch(a_handleType)
			{
				case ImageWarpHandleType.CornerTL:
					SetCornerWorldPositionTL (a_worldPosition); break;
				case ImageWarpHandleType.CornerTR:
					SetCornerWorldPositionTR (a_worldPosition); break;
				case ImageWarpHandleType.CornerBR:
					SetCornerWorldPositionBR (a_worldPosition); break;
				case ImageWarpHandleType.CornerBL:
					SetCornerWorldPositionBL (a_worldPosition); break;

				case ImageWarpHandleType.TopBezierHandleA:
					SetTopBezierWorldPositionHandleA (a_worldPosition); break;
				case ImageWarpHandleType.TopBezierHandleB:
					SetTopBezierWorldPositionHandleB (a_worldPosition); break;
				case ImageWarpHandleType.RightBezierHandleA:
					SetRightBezierWorldPositionHandleA (a_worldPosition); break;
				case ImageWarpHandleType.RightBezierHandleB:
					SetRightBezierWorldPositionHandleB (a_worldPosition); break;
				case ImageWarpHandleType.BottomBezierHandleA:
					SetBottomBezierWorldPositionHandleA (a_worldPosition); break;
				case ImageWarpHandleType.BottomBezierHandleB:
					SetBottomBezierWorldPositionHandleB (a_worldPosition); break;
				case ImageWarpHandleType.LeftBezierHandleA:
					SetLeftBezierWorldPositionHandleA (a_worldPosition); break;
				case ImageWarpHandleType.LeftBezierHandleB:
					SetLeftBezierWorldPositionHandleB (a_worldPosition); break;
			}
		}

		public void SetHandleLocalPosition(ImageWarpHandleType a_handleType, Vector3 a_localPosition)
		{
			switch(a_handleType)
			{
				case ImageWarpHandleType.CornerTL:
					SetCornerLocalPositionTL (a_localPosition); break;
				case ImageWarpHandleType.CornerTR:
					SetCornerLocalPositionTR (a_localPosition); break;
				case ImageWarpHandleType.CornerBR:
					SetCornerLocalPositionBR (a_localPosition); break;
				case ImageWarpHandleType.CornerBL:
					SetCornerLocalPositionBL (a_localPosition); break;

				case ImageWarpHandleType.TopBezierHandleA:
					SetTopBezierLocalPositionHandleA (a_localPosition); break;
				case ImageWarpHandleType.TopBezierHandleB:
					SetTopBezierLocalPositionHandleB (a_localPosition); break;
				case ImageWarpHandleType.RightBezierHandleA:
					SetRightBezierLocalPositionHandleA (a_localPosition); break;
				case ImageWarpHandleType.RightBezierHandleB:
					SetRightBezierLocalPositionHandleB (a_localPosition); break;
				case ImageWarpHandleType.BottomBezierHandleA:
					SetBottomBezierLocalPositionHandleA (a_localPosition); break;
				case ImageWarpHandleType.BottomBezierHandleB:
					SetBottomBezierLocalPositionHandleB (a_localPosition); break;
				case ImageWarpHandleType.LeftBezierHandleA:
					SetLeftBezierLocalPositionHandleA (a_localPosition); break;
				case ImageWarpHandleType.LeftBezierHandleB:
					SetLeftBezierLocalPositionHandleB (a_localPosition); break;
			}
		}


		[SerializeField]
		private WarpManager m_warpManager;
		public WarpManager warpManager { get { return m_warpManager; } }

		private WarpManager.WarpManagerInstanceData m_warpManagerData;
		private WarpManager.WarpManagerMeshData m_warpedMeshData;

		private List<UIVertex> m_meshVerts;
		private Vector3[] m_meshVertsVec3;
		private Vector2[] m_meshUvsVec2;

		private RectTransform m_rectTransform;
		public RectTransform RectTransformComponent {
			get {
				if (m_rectTransform == null)
				{
					m_rectTransform = rectTransform;
				}
				return m_rectTransform;
			}
		}

		private Transform m_transform;
		public Transform TransformComponent {
			get {
				if (m_transform == null)
					m_transform = transform;
				return m_transform;
			}
		}

		bool SetStruct<T>(ref T currentValue, T newValue) where T : struct
		{
			if (currentValue.Equals(newValue))
				return false;

			currentValue = newValue;
			return true;
		}

		protected override void OnPopulateMesh(VertexHelper vh)
		{
			// get default mesh positions
			base.OnPopulateMesh(vh);

			if (m_warpManager == null)
			{
				m_warpManager = new WarpManager ();
			}

			if (m_warpedMeshData == null)
			{
				m_warpedMeshData = new WarpManager.WarpManagerMeshData ();
			}

			PopulateWarpManagerData ();

			if (m_meshVerts == null)
			{
				m_meshVerts = new List<UIVertex> ();
				m_meshVertsVec3 = new Vector3[4];
				m_meshUvsVec2 = new Vector2[4];
			}

			// Grab the default calculated mesh vert positions
			vh.GetUIVertexStream (m_meshVerts);

			// Populate vector3 array of mesh vert positions
			m_meshVertsVec3 [0] = m_meshVerts [0].position;
			m_meshVertsVec3 [1] = m_meshVerts [1].position;
			m_meshVertsVec3 [2] = m_meshVerts [2].position;
			m_meshVertsVec3 [3] = m_meshVerts [4].position;

			// Populate vector2 array of mesh vert uvs
			m_meshUvsVec2 [0] = uvRect.min; // BL
			m_meshUvsVec2 [1] = uvRect.min + new Vector2(0, uvRect.height); // TL
			m_meshUvsVec2 [2] = uvRect.max; // TR
			m_meshUvsVec2 [3] = uvRect.min + new Vector2(uvRect.width, 0); // BR

			// Get warp mesh data
			m_warpManager.PopulateMesh (m_meshVertsVec3, m_meshUvsVec2, m_warpManagerData, ref m_warpedMeshData);


			// Repopulate mesh data with Warped data
			vh.Clear();

			for (int idx = 0; idx < m_warpedMeshData.m_positions.Length; idx++)
			{
				vh.AddVert(m_warpedMeshData.m_positions[idx], color, m_warpedMeshData.m_uvs[idx]);
			}

			for (int idx = 0; idx < m_warpedMeshData.m_indices.Length; idx += 3)
			{
				vh.AddTriangle(m_warpedMeshData.m_indices[idx], m_warpedMeshData.m_indices[idx + 1], m_warpedMeshData.m_indices[idx + 2]);
			}
		}

		private void PopulateWarpManagerData()
		{
			if (m_warpManagerData == null)
			{
				m_warpManagerData = new WarpManager.WarpManagerInstanceData ();
			}

			m_warpManagerData.m_flipX = m_flipX;
			m_warpManagerData.m_flipY = m_flipY;

			m_warpManagerData.m_bezierEdges = m_bezierEdges;
			m_warpManagerData.m_numSubdivisions = m_numSubdivisions;

			m_warpManagerData.m_cornerOffsetBL = m_cornerOffsetBL;
			m_warpManagerData.m_cornerOffsetTL = m_cornerOffsetTL;
			m_warpManagerData.m_cornerOffsetTR = m_cornerOffsetTR;
			m_warpManagerData.m_cornerOffsetBR = m_cornerOffsetBR;

			m_warpManagerData.m_bottomCurveHandleA = m_bottomBezierHandleA;
			m_warpManagerData.m_bottomCurveHandleB = m_bottomBezierHandleB;
			m_warpManagerData.m_leftCurveHandleA = m_leftBezierHandleA;
			m_warpManagerData.m_leftCurveHandleB = m_leftBezierHandleB;
			m_warpManagerData.m_rightCurveHandleA = m_rightBezierHandleA;
			m_warpManagerData.m_rightCurveHandleB = m_rightBezierHandleB;
			m_warpManagerData.m_topCurveHandleA = m_topBezierHandleA;
			m_warpManagerData.m_topCurveHandleB = m_topBezierHandleB;

			m_warpManagerData.m_cropLeft = m_cropLeft;
			m_warpManagerData.m_cropTop = m_cropTop;
			m_warpManagerData.m_cropRight = m_cropRight;
			m_warpManagerData.m_cropBottom = m_cropBottom;
		}

		public void ForceUpdateGeometry()
		{
			UpdateGeometry ();

#if UNITY_EDITOR
			if(!Application.isPlaying)
			{
				UnityEditor.EditorUtility.SetDirty(this);
			}
#endif
		}

		public void ResetAll()
		{
			m_warpManager.ResetAll (this);

			ForceUpdateGeometry ();
		}

		public void ResetCropping()
		{
			m_warpManager.ResetCropping (this);

			ForceUpdateGeometry ();
		}

		public void ResetCornerOffsets()
		{
			m_warpManager.ResetCornerOffsets (this);

			ForceUpdateGeometry ();
		}

		public void ResetBezierHandlesToDefault()
		{
			m_warpManager.ResetBezierHandlesToDefault (this);

			ForceUpdateGeometry ();
		}

#if UNITY_EDITOR

		void OnDrawGizmosSelected()
		{
			if (m_warpManager == null)
				return;

			m_warpManager.OnDrawGizmos (TransformComponent, m_warpedMeshData);
		}

		[MenuItem ("Tools/Image Warp/Convert UI Raw Image to UI Raw Image Warp", false, 201)]
		static void ConvertToUIRawImageWarp ()
		{
			if (WarpHelper.ConvertRawImageToRawImageWarp (Selection.activeGameObject))
			{
				Debug.Log (Selection.activeGameObject.name + "'s RawImage component converted into a UIRawImageWarp component");
			}
		}

		[MenuItem ("Tools/Image Warp/Convert UI Raw Image to UI Raw Image Warp", true)]
		static bool ValidateConvertToUIRawImageWarp ()
		{
			if (Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<RawImage> () != null)
			{
				return true;
			}
			else
			{
				return false;
			}
		}
#endif
	}
}