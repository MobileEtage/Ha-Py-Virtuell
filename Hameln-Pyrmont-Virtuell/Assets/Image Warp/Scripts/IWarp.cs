using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fenderrio.ImageWarp
{
	public interface IWarp
	{
		// Properties
		bool flipX { get; set; }
		bool flipY { get; set; }

		Vector3 cornerOffsetTL { get; set; }
		Vector3 cornerOffsetTR { get; set; }
		Vector3 cornerOffsetBL { get; set; }
		Vector3 cornerOffsetBR { get; set; }

		int numSubdivisions { get; set; }
		bool bezierEdges { get; set; }

		Vector3 topBezierHandleA { get; set; }
		Vector3 topBezierHandleB { get; set; }
		Vector3 rightBezierHandleA { get; set; }
		Vector3 rightBezierHandleB { get; set; }
		Vector3 bottomBezierHandleA { get; set; }
		Vector3 bottomBezierHandleB { get; set; }
		Vector3 leftBezierHandleA { get; set; }
		Vector3 leftBezierHandleB { get; set; }

		float cropLeft { get; set; }
		float cropTop { get; set; }
		float cropRight { get; set; }
		float cropBottom { get; set; }

		Vector3 cornerWorldPositionTL { get; }
		Vector3 cornerLocalPositionTL { get; }
		Vector3 cornerWorldPositionTR { get; }
		Vector3 cornerLocalPositionTR { get; }
		Vector3 cornerWorldPositionBR { get; }
		Vector3 cornerLocalPositionBR { get; }
		Vector3 cornerWorldPositionBL { get; }
		Vector3 cornerLocalPositionBL { get; }

		Vector3 topBezierWorldPositionHandleA { get; }
		Vector3 topBezierLocalPositionHandleA { get; }
		Vector3 topBezierWorldPositionHandleB { get; }
		Vector3 topBezierLocalPositionHandleB { get; }

		Vector3 rightBezierWorldPositionHandleA { get; }
		Vector3 rightBezierLocalPositionHandleA { get; }
		Vector3 rightBezierWorldPositionHandleB { get; }
		Vector3 rightBezierLocalPositionHandleB { get; }

		Vector3 bottomBezierWorldPositionHandleA { get; }
		Vector3 bottomBezierLocalPositionHandleA { get; }
		Vector3 bottomBezierWorldPositionHandleB { get; }
		Vector3 bottomBezierLocalPositionHandleB { get; }

		Vector3 leftBezierWorldPositionHandleA { get; }
		Vector3 leftBezierLocalPositionHandleA { get; }
		Vector3 leftBezierWorldPositionHandleB { get; }
		Vector3 leftBezierLocalPositionHandleB { get; }

		// Convenience methods
		void ResetAll ();
		void ResetCropping ();
		void ResetCornerOffsets ();
		void ResetBezierHandlesToDefault ();

		void SetCornerWorldPositionTL (Vector3 a_worldPosition);
		void SetCornerLocalPositionTL (Vector3 a_localPosition);
		void SetCornerWorldPositionTR (Vector3 a_worldPosition);
		void SetCornerLocalPositionTR (Vector3 a_localPosition);
		void SetCornerWorldPositionBR (Vector3 a_worldPosition);
		void SetCornerLocalPositionBR (Vector3 a_localPosition);
		void SetCornerWorldPositionBL (Vector3 a_worldPosition);
		void SetCornerLocalPositionBL (Vector3 a_localPosition);

		void SetTopBezierWorldPositionHandleA (Vector3 a_worldPosition);
		void SetTopBezierLocalPositionHandleA (Vector3 a_localPosition);
		void SetTopBezierWorldPositionHandleB (Vector3 a_worldPosition);
		void SetTopBezierLocalPositionHandleB (Vector3 a_localPosition);
		void SetRightBezierWorldPositionHandleA (Vector3 a_worldPosition);
		void SetRightBezierLocalPositionHandleA (Vector3 a_localPosition);
		void SetRightBezierWorldPositionHandleB (Vector3 a_worldPosition);
		void SetRightBezierLocalPositionHandleB (Vector3 a_localPosition);
		void SetBottomBezierWorldPositionHandleA (Vector3 a_worldPosition);
		void SetBottomBezierLocalPositionHandleA (Vector3 a_localPosition);
		void SetBottomBezierWorldPositionHandleB (Vector3 a_worldPosition);
		void SetBottomBezierLocalPositionHandleB (Vector3 a_localPosition);
		void SetLeftBezierWorldPositionHandleA (Vector3 a_worldPosition);
		void SetLeftBezierLocalPositionHandleA (Vector3 a_localPosition);
		void SetLeftBezierWorldPositionHandleB (Vector3 a_worldPosition);
		void SetLeftBezierLocalPositionHandleB (Vector3 a_localPosition);

		void SetHandleWorldPosition (ImageWarpHandleType a_handleType, Vector3 a_worldPosition);
		void SetHandleLocalPosition (ImageWarpHandleType a_handleType, Vector3 a_localPosition);

		Vector3 GetHandleWorldPosition (ImageWarpHandleType a_handleType);
		Vector3 GetHandleLocalPosition (ImageWarpHandleType a_handleType);
	}
}