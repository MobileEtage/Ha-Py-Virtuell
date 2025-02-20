using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pigeon : MonoBehaviour
{
	public Renderer myRenderer;
	public GameObject myLight;
	public GameObject shadowPlane;
	private float moveSpeed = 0.6f;
	public bool targetReached = false;

	private Color rendererColor;
	private Color rendererTargetColor;
	private float fadeInDelay = 0.1f;
	private float fadeInTime = 0.75f;
	public float currentFadeInTime = 0.0f;

	private List<Vector3> pathPoints = new List<Vector3>();
	private int nextPathPoint = 0;
	private float rotateSpeed = 5.0f;
	private float waitDelay = 0.0f;
	private float initialDelay = 3.0f;
	private float crossfadeTime = 0.1f;
	private float moveDelay = 0.0f;
	private int targetPointsReached = 0;
	private bool movementEnabled = false;

	private float targetRadius = 5f;
	private float targetRadiusOffset = 0f;

	private float targetYPosition = 0.0f;
	private float heightWaveFactor = 0.2f;
	private float heightWaveFactor2 = 0.25f;
	private bool isFalling = false;

	// 0: idle
	// 1: walk
	// 2: fly
	// 3: glide
	// 4: takeoff
	public int animationState = 0;

	void Awake()
	{
		rendererColor = myRenderer.materials[0].color;
		rendererColor.a = 0;

		Material[] mats = myRenderer.materials;
		for (int i = 0; i < mats.Length; i++){ mats[i].color = rendererColor; }
		myRenderer.materials = mats;
		transform.GetChild(2).GetComponent<Renderer>().material.SetFloat("_ShadowIntensity", 0);

		rendererTargetColor = rendererColor;
		rendererTargetColor.a = 1.0f;
	}

	void Update()
	{
		if (fadeInDelay <= 0)
		{

			if (currentFadeInTime < fadeInTime)
			{
				float percentage = currentFadeInTime / fadeInTime;
				Material[] mats = myRenderer.materials;
				for (int i = 0; i < mats.Length; i++) { mats[i].color = Color.Lerp(rendererColor, rendererTargetColor, percentage);	}
				myRenderer.materials = mats;
				shadowPlane.GetComponent<Renderer>().material.SetFloat("_ShadowIntensity", percentage * 0.5f);

				currentFadeInTime += Time.deltaTime;
                if (currentFadeInTime >= fadeInTime)
                {
                    for (int i = 0; i < mats.Length; i++) { mats[i].color = rendererTargetColor; }
					myRenderer.materials = mats;
					shadowPlane.GetComponent<Renderer>().material.SetFloat("_ShadowIntensity", 0.5f);

					string layerName = "Default";
					ToolsController.instance.ChangeLayer(this.gameObject, layerName);

					//myLight.GetComponentInChildren<Light>(true).gameObject.SetActive(false);
					//shadowPlane.SetActive(false);

					Destroy(myLight);
					Destroy(shadowPlane);
				}
            }
		}
		else
		{
			fadeInDelay -= Time.deltaTime;
		}

		UpdateBehaviour();
	}

	public void OnDestroy()
    {
		Destroy(myLight);
		Destroy(shadowPlane);
    }

	// Create some path points
	public void InitMovement(bool isFlying)
	{

		List<Vector3> path = new List<Vector3>();
		if (isFlying)
		{

			GetComponentInChildren<Animator>().Play("fly");
			animationState = 2;
			path = CreateCircleFlyPath();
			initialDelay = 0.6f;

		}
		else
		{

			GetComponentInChildren<Animator>().Play("idle");
			animationState = 0;

			Vector3 pos = GetRandomPositionOnGround();
			path.Add(pos);
		}

		StartMove(path);
	}

	public List<Vector3> CreateCircleFlyPath()
	{

		List<Vector3> path = new List<Vector3>();
		List<Vector3> tmpPath = new List<Vector3>();

		Vector3 startPosition = transform.position;
		float angle = 0;
		float radius = targetRadius + Random.Range(-1, 3);
		float heightOffset = Random.Range(0, 3);
		float dir = Random.value > 0.5f ? 1 : -1;
		float angleOffset = Random.Range(0, 360);

		targetYPosition = ViewingPigeonsController.instance.mainCamera.transform.position.y + 0.0f;
		int steps = 60;

		for (int i = 0; i < steps; i++)
		{

			angle = (i / (float)steps) * 360f + angleOffset;
			float x = radius * Mathf.Cos(dir * angle * Mathf.Deg2Rad);
			float z = radius * Mathf.Sin(dir * angle * Mathf.Deg2Rad);

			Vector3 pos = startPosition + new Vector3(x, 0, z);
			targetYPosition += Mathf.Sin(i * heightWaveFactor) * heightWaveFactor2;
			pos.y = targetYPosition + heightOffset;

			tmpPath.Add(pos);
		}

		int circleCount = Random.Range(2, 4);
		for (int i = 0; i < circleCount; i++)
		{

			for (int j = 0; j < tmpPath.Count; j++)
			{
				path.Add(tmpPath[j]);
			}
		}

		Vector3 randomFarAwayPositon = ViewingPigeonsController.instance.mainCamera.transform.position +
			Random.insideUnitSphere * 50;
		randomFarAwayPositon.y = ViewingPigeonsController.instance.mainCamera.transform.position.y;
		path.Add(randomFarAwayPositon);

		return path;
	}

	public Vector3 GetRandomPositionOnGround()
	{

		Vector3 startPosition = this.transform.position;
		Vector3 forward = new Vector3(this.transform.forward.x, 0, this.transform.forward.z);
		Vector3 right = new Vector3(this.transform.right.x, 0, this.transform.right.z);
		Vector3 pos = startPosition +
			forward * Random.Range(0.75f, 1.5f) +
			right * Random.Range(0.25f, 0.5f);

		pos.y = startPosition.y;

		return pos;
	}

	public void StartMove(List<Vector3> pathPoints)
	{

		this.pathPoints = pathPoints;
		movementEnabled = true;
	}

	public void Move(Transform movingObject)
	{

		if (!movementEnabled) return;
		if (pathPoints.Count <= 0) return;

		float speedFactor = 1.0f;
		if (animationState == 2)
		{
			speedFactor = 2.0f;
		}
		else if (animationState == 3)
		{
			speedFactor = 2.35f;
		}
		else if (animationState == 4)
		{
			speedFactor = 1.2f;
		}
		else
		{
			speedFactor = 0.4f;
		}

		// Move and rotate towards next path point
		if (nextPathPoint < pathPoints.Count)
		{

			Vector3 targetPosition = pathPoints[nextPathPoint];

			/******** Set rotation ********/
			Vector3 lookAtPoint = targetPosition;

			var direction = lookAtPoint - movingObject.position;
			if (direction != Vector3.zero)
			{
				//direction.y = 0;
				direction = direction.normalized;
				var rotation = Quaternion.LookRotation(direction);
				//movingObject.rotation = Quaternion.Slerp(
				movingObject.rotation = Quaternion.Lerp(
					movingObject.rotation, rotation, rotateSpeed * Time.deltaTime);
			}

			/******** Set position ********/
			movingObject.localPosition = Vector3.MoveTowards(
				movingObject.localPosition, targetPosition, moveSpeed * speedFactor * Time.deltaTime);

			/******** Update target point ********/
			float dist = Vector3.Distance(movingObject.localPosition, pathPoints[nextPathPoint]);
			if (dist < 0.1f)
			{
				OnTargetPointReached();
			}
		}
	}

	public void OnTargetPointReached()
	{

		targetPointsReached++;

		if (animationState == 1)
		{

			float random = Random.value;
			if (random < 0.33f)
			{
				GetComponentInChildren<Animator>().CrossFade("idle", crossfadeTime);
			}
			else if (random < 0.66f)
			{
				GetComponentInChildren<Animator>().CrossFade("eat", crossfadeTime);
			}
			else
			{
				GetComponentInChildren<Animator>().CrossFade("preen", crossfadeTime);
			}

			animationState = 0;

			if (targetPointsReached < 4)
			{

				waitDelay = Random.Range(3.0f, 6.0f);

				Vector3 pos = GetRandomPositionOnGround();
				pathPoints[0] = pos;
				nextPathPoint = 0;
			}
			else
			{

				// Switch to flying
				GetComponentInChildren<Animator>().Play("takeoff");
				animationState = 4;
				pathPoints = CreateCircleFlyPath();
				nextPathPoint = 0;
				waitDelay = Random.Range(1.5f, 3.0f);

				//targetReached = true;
			}
		}
		else if (animationState == 2 || animationState == 3 || animationState == 4)
		{

			nextPathPoint++;

			if (nextPathPoint < pathPoints.Count)
			{

				if (!isFalling && pathPoints[nextPathPoint].y < pathPoints[nextPathPoint - 1].y &&
					nextPathPoint != 0
				)
				{

					animationState = 3;
					GetComponentInChildren<Animator>().CrossFade("glide", 0.2f);
					isFalling = true;
				}
				else if (isFalling && pathPoints[nextPathPoint].y > pathPoints[nextPathPoint - 1].y)
				{

					animationState = 2;
					GetComponentInChildren<Animator>().CrossFade("fly", 0.2f);
					isFalling = false;
				}

			}
			else
			{

				GetComponentInChildren<Animator>().CrossFade("fly", 0.2f);
				animationState = 2;

				targetReached = true;
			}
		}
	}


	public void UpdateBehaviour()
	{

		if (targetReached) return;
		if (initialDelay > 0)
		{
			initialDelay -= Time.deltaTime;
			return;
		}
		Wait();

		if (animationState == 0) return;
		if (moveDelay > 0) { moveDelay -= Time.deltaTime; return; }
		Move(this.transform);
	}

	public void Wait()
	{

		if (waitDelay > 0)
		{
			waitDelay -= Time.deltaTime;
		}
		else
		{

			if (animationState == 0)
			{

				float random = Random.value;
				if (random < 0.5f)
				{
					GetComponentInChildren<Animator>().CrossFade("walk", crossfadeTime);
				}
				else
				{
					GetComponentInChildren<Animator>().CrossFade("walk", crossfadeTime);
				}
				animationState = 1;
				moveDelay = 0.4f;
			}

			if (animationState == 3 || animationState == 2 || animationState == 4)
			{

				/*
				int switchState = (animationState == 2) ? 3:2;
				bool glide = false;
				if( switchState == 2 ){
					GetComponentInChildren<Animator>().CrossFade("fly", 0.2f);
				}else{
					GetComponentInChildren<Animator>().CrossFade("glide", 0.2f);
					glide = true;
				}
				animationState = switchState;
				
				if( glide ){
					waitDelay = Random.Range(1.0f, 2.0f);
				}else{
					waitDelay = Random.Range(3.0f, 6.0f);					
				}
				*/
			}
		}
	}

}
