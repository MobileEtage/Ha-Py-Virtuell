using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CarouselSilder : MonoBehaviour
{
	public bool useScrollRect = true;
	public bool infiniteSlider = true;

	public float swipeTime = 0.25f;
	public float scaleStep = 0.25f;
	public int selectedIndex = 0;
	public int elements = 7;
	public int visibleElements = 5;
	public GameObject dotPrefab;
	public GameObject dotHolder;
	public ScrollRect scrollRect;
	
	public List<GameObject> dots = new List<GameObject>();
	private GameObject additionalDot;
	private bool isSwiping = false;
	private float xSpacing = 1;
	public int currentVisibleIndex = 0;
	private int scrollRightMaxPos = 2;
	
	void Start(){
		
		//Init( visibleElements, elements );
		//SetDirection(-1);
	}

	void Update(){
		
		#if UNITY_EDITOR
		if( Input.GetKeyDown(KeyCode.LeftArrow) ){
			Swipe(-1);
		}
		if( Input.GetKeyDown(KeyCode.RightArrow) ){
			Swipe(1);
		}
		#endif
	}
	
	public void Init( int visibleElementsCount, int elementsCount ){
				
		if( useScrollRect ){
			
			if( elementsCount < 3 ){
				infiniteSlider = false;
			}
			
			visibleElements = Mathf.Clamp(elementsCount, 0, visibleElementsCount);
			elements = visibleElements;
			
			if( infiniteSlider ){
				elements = visibleElements+2;
			}
			
			GetComponent<RectTransform>().sizeDelta = new Vector2(
				visibleElements*dotHolder.GetComponent<GridLayoutGroup>().cellSize.x + 
				(visibleElements-1)*dotHolder.GetComponent<GridLayoutGroup>().spacing.x, GetComponent<RectTransform>().sizeDelta.y );
				
			/*
			scrollRect.GetComponent<RectTransform>().sizeDelta = new Vector2(
				visibleElements*dotHolder.GetComponent<GridLayoutGroup>().cellSize.x + 
				(visibleElements-1)*dotHolder.GetComponent<GridLayoutGroup>().spacing.x, scrollRect.GetComponent<RectTransform>().sizeDelta.y );
			*/
			
			if( !infiniteSlider ){
				currentVisibleIndex = -1;
			}
			if( visibleElements > 4 ){
				scrollRightMaxPos = visibleElements-3;
			}else if( visibleElements == 4 ){
				scrollRightMaxPos = 1;
			}else{
				scrollRightMaxPos = 0;
			}
			CreateSliderScrollRect();	
			
		}else{
			CreateSlider();
		}
		xSpacing = dotHolder.GetComponent<GridLayoutGroup>().spacing.x +  dotHolder.GetComponent<GridLayoutGroup>().cellSize.x;
	}
	
	public void CreateSliderScrollRect(){
		
		foreach( Transform child in dotHolder.transform ){
			Destroy( child.gameObject );
		}
		dots.Clear();
		
		if( elements <= 1 ) return;
		
		for( int i = 0; i < elements; i++ ){
			
			GameObject dot = Instantiate(dotPrefab);
			dot.SetActive(true);
			dot.transform.SetParent( dotHolder.transform );
			dot.transform.localScale = Vector3.one;
			dot.GetComponent<RectTransform>().anchoredPosition3D = Vector3.zero;
			dot.GetComponent<CarouselSilderDot>().indexID = i;
			dots.Add(dot);
		}
		
		UpdateSelection( selectedIndex );
	}
	
	public void UpdateSelection( int index ){

		for( int i = 0; i < dots.Count; i++ ){
			
			dots[i].GetComponent<CarouselSilderDot>().circle.localScale = Vector3.zero;
			SetScale( dots[i].GetComponent<CarouselSilderDot>(), 1);
		}
		dots[selectedIndex].GetComponent<CarouselSilderDot>().circle.localScale = Vector3.one;
		SetScale( dots[selectedIndex].GetComponent<CarouselSilderDot>(), 2);
		
		dotHolder.transform.GetChild( dotHolder.transform.childCount-1 ).transform.SetSiblingIndex(0);
	}
	
	public GameObject GetDotById( int id ){
		
		for( int i = 0; i < dots.Count; i++ ){		
			if( dots[i].GetComponent<CarouselSilderDot>().indexID == id ) return dots[i];
		}
		return null;
	}
	
	public float GetScrollStep(){
		
		float scrollStep = 0;
		if( elements > visibleElements ) {
			scrollStep = 1f/(elements-visibleElements);
		}

		return scrollStep;
	}
	
	public float GetScrollPosition( int index ){

		return 0;
	}
	
	public void CreateSlider(){
		
		foreach( Transform child in dotHolder.transform ){
			Destroy( child.gameObject );
		}
		dots.Clear();
		
		if( elements <= 1 ) return;
		
		int count = Mathf.Clamp( visibleElements+1, 0, elements+1 );
		for( int i = 0; i < count; i++ ){
			GameObject dot = Instantiate(dotPrefab);
			dot.SetActive(true);
			dot.transform.SetParent( dotHolder.transform );
			dot.transform.localScale = Vector3.one;
			dot.GetComponent<RectTransform>().anchoredPosition3D = Vector3.zero;
			if( i != count-1 ){
				dots.Add(dot);
			}else{
				additionalDot = dot;
				additionalDot.transform.SetParent(this.transform);
				additionalDot.SetActive(false);
			}
		}
				
		for( int i = 0; i < dots.Count; i++ ){
			
			if( i == 0 || i == dots.Count-1 ){
				SetScale( dots[i].GetComponent<CarouselSilderDot>(), 0);
			}
			
			if( visibleElements >= 5 ){
				if( i == 1 || i == dots.Count-2 ){
					SetScale( dots[i].GetComponent<CarouselSilderDot>(), 1);
				}
			}
		}
		
		if( dots.Count <= 3 ){
			for( int i = 0; i < dots.Count; i++ ){
				SetScale( dots[i].GetComponent<CarouselSilderDot>(), 1);
			}
		}
		
		dots[selectedIndex].GetComponent<CarouselSilderDot>().circle.localScale = Vector3.one;
		SetScale( dots[selectedIndex].GetComponent<CarouselSilderDot>(), 2);
	}
	
	public void SetScale( CarouselSilderDot carouselSilderDot, int scaleType ){
		
		if( scaleType == 0 ){
			carouselSilderDot.root.localScale = Vector3.one * (1-2*scaleStep );
		}else if( scaleType == 1 ){
			carouselSilderDot.root.localScale = Vector3.one * (1-1*scaleStep );
		}else{
			carouselSilderDot.root.localScale = Vector3.one;
		}
	}
	
	public void Swipe( int direction ){
		
		if( elements <= 1 ) return;
		if(isSwiping) return;
		isSwiping = true;
		
		if( useScrollRect ){
			StartCoroutine( SwipeCoroutineScrollRect(direction) );
		}else{
			StartCoroutine( SwipeCoroutine(direction) );
		}
	}
	
	public IEnumerator SwipeCoroutine( int direction ){
		
		if( dots.Count <= 3 ){
			
			dots[selectedIndex].GetComponent<CarouselSilderDot>().circle.gameObject.SetActive(false);
			dots[selectedIndex].GetComponent<CarouselSilderDot>().circle.localScale = Vector3.zero;
			StartCoroutine( 
				AnimationController.instance.AnimateScaleCoroutine(
				dots[selectedIndex].GetComponent<CarouselSilderDot>().root,
				dots[selectedIndex].GetComponent<CarouselSilderDot>().root.localScale,
				Vector3.one * (1-1*scaleStep ),
				swipeTime,
				"smooth"
				));
				
			selectedIndex += direction;
			if( selectedIndex >= dots.Count ) selectedIndex = 0;
			if( selectedIndex < 0 ) selectedIndex = dots.Count - 1;
				
			dots[selectedIndex].GetComponent<CarouselSilderDot>().circle.gameObject.SetActive(true);
			dots[selectedIndex].GetComponent<CarouselSilderDot>().circle.localScale = Vector3.one;
			yield return StartCoroutine( 
				AnimationController.instance.AnimateScaleCoroutine(
				dots[selectedIndex].GetComponent<CarouselSilderDot>().root,
				dots[selectedIndex].GetComponent<CarouselSilderDot>().root.localScale,
				Vector3.one,
				swipeTime,
				"smooth"
				));
				
			
		}
		else{

		}
		
		isSwiping = false;
	}
	
	public void SetDirection( int direction ){
		
		bool move = false;
		if( direction == 1 ){
			if( currentVisibleIndex == scrollRightMaxPos ){
				move = true;
			}
		}else{
			if( currentVisibleIndex == 0 ){
				move = true;
			}
		}
		
		if( currentVisibleIndex == -1 ){
			move = false;
		}
		
		if( direction == -1 && move){
			
			if( GetDotById( selectedIndex ).transform.GetSiblingIndex() == 1 ){
				dotHolder.transform.GetChild( dotHolder.transform.childCount-1 ).transform.SetSiblingIndex(0);
				scrollRect.horizontalNormalizedPosition += GetScrollStep();
			}
			
			// Scroll
			float targetScroll = scrollRect.horizontalNormalizedPosition+direction*GetScrollStep();	
			scrollRect.horizontalNormalizedPosition = targetScroll;			
			
			// Scale current down
			dots[selectedIndex].GetComponent<CarouselSilderDot>().circle.gameObject.SetActive(false);
			dots[selectedIndex].GetComponent<CarouselSilderDot>().circle.localScale = Vector3.zero;
			dots[selectedIndex].GetComponent<CarouselSilderDot>().root.localScale = Vector3.one * (1-1*scaleStep );
				
			// Scale element at other end of scroll rect down
			int childIndexDotScaleDown = GetScaleDownElementIndex(direction);
			dotHolder.transform.GetChild( childIndexDotScaleDown ).GetComponent<CarouselSilderDot>().root.localScale = Vector3.zero;
				
			// Scale element at other end of scroll rect up
			int childIndexDotScaleUp = GetScaleUpElementIndex( direction );	
			dotHolder.transform.GetChild( childIndexDotScaleUp ).GetComponent<CarouselSilderDot>().root.localScale = Vector3.one * (1-1*scaleStep );
				
			// Change selected indx
			selectedIndex += direction;
			if( selectedIndex >= dots.Count ) selectedIndex = 0;
			if( selectedIndex < 0 ) selectedIndex = dots.Count - 1;
			
			// Scale new selected up
			dots[selectedIndex].GetComponent<CarouselSilderDot>().circle.gameObject.SetActive(true);
			dots[selectedIndex].GetComponent<CarouselSilderDot>().circle.localScale = Vector3.one;
			dots[selectedIndex].GetComponent<CarouselSilderDot>().root.localScale = Vector3.one;

			// Reset scale of element at other end of scroll rect
			dotHolder.transform.GetChild( childIndexDotScaleDown ).GetComponent<CarouselSilderDot>().root.localScale = Vector3.one * (1-1*scaleStep );

				
		}else if( direction == 1 && move ){
			
			if( GetDotById( selectedIndex ).transform.GetSiblingIndex() == dotHolder.transform.childCount-2 ){
				dotHolder.transform.GetChild( 0 ).transform.SetSiblingIndex( dotHolder.transform.childCount-1 );
				scrollRect.horizontalNormalizedPosition -= GetScrollStep();
			}
			
			// Scroll
			float targetScroll = scrollRect.horizontalNormalizedPosition+direction*GetScrollStep();	
			scrollRect.horizontalNormalizedPosition = targetScroll;	
			
			// Scale
			dots[selectedIndex].GetComponent<CarouselSilderDot>().circle.gameObject.SetActive(false);
			dots[selectedIndex].GetComponent<CarouselSilderDot>().circle.localScale = Vector3.zero;
			dots[selectedIndex].GetComponent<CarouselSilderDot>().root.localScale = Vector3.one * (1-1*scaleStep );
				
			// Scale element at other end of scroll rect down
			int childIndexDotScaleDown = GetScaleDownElementIndex( direction );	
			dotHolder.transform.GetChild( childIndexDotScaleDown ).GetComponent<CarouselSilderDot>().root.localScale = Vector3.zero;
				
			// Scale element at other end of scroll rect up
			int childIndexDotScaleUp = GetScaleUpElementIndex( direction );	
			dotHolder.transform.GetChild( childIndexDotScaleUp ).GetComponent<CarouselSilderDot>().root.localScale = Vector3.one * (1-1*scaleStep );
				
			selectedIndex += direction;
			if( selectedIndex >= dots.Count ) selectedIndex = 0;
			if( selectedIndex < 0 ) selectedIndex = dots.Count - 1;
			
			dots[selectedIndex].GetComponent<CarouselSilderDot>().circle.gameObject.SetActive(true);
			dots[selectedIndex].GetComponent<CarouselSilderDot>().circle.localScale = Vector3.one;
			dots[selectedIndex].GetComponent<CarouselSilderDot>().root.localScale = Vector3.one;
				
			// Reset scale of element at other end of scroll rect
			dotHolder.transform.GetChild( childIndexDotScaleDown ).GetComponent<CarouselSilderDot>().root.localScale = Vector3.one * (1-1*scaleStep );
			
		}else{
			
			if( currentVisibleIndex != -1 ){
				currentVisibleIndex += direction;
			}
			
			dots[selectedIndex].GetComponent<CarouselSilderDot>().circle.gameObject.SetActive(false);
			dots[selectedIndex].GetComponent<CarouselSilderDot>().circle.localScale = Vector3.zero;
			dots[selectedIndex].GetComponent<CarouselSilderDot>().root.localScale = Vector3.one * (1-1*scaleStep );
				
			selectedIndex += direction;
			if( selectedIndex >= dots.Count ) selectedIndex = 0;
			if( selectedIndex < 0 ) selectedIndex = dots.Count - 1;
				
			dots[selectedIndex].GetComponent<CarouselSilderDot>().circle.gameObject.SetActive(true);
			dots[selectedIndex].GetComponent<CarouselSilderDot>().circle.localScale = Vector3.one;
			dots[selectedIndex].GetComponent<CarouselSilderDot>().root.localScale = Vector3.one;
		}
		
		isSwiping = false;
	}
	
	public IEnumerator SwipeCoroutineScrollRect( int direction ){
		
		bool move = false;
		if( direction == 1 ){
			if( currentVisibleIndex == scrollRightMaxPos ){
				move = true;
			}
		}else{
			if( currentVisibleIndex == 0 ){
				move = true;
			}
		}
		
		if( currentVisibleIndex == -1 ){
			move = false;
		}
		
		if( direction == -1 && move){
			
			if( GetDotById( selectedIndex ).transform.GetSiblingIndex() == 1 ){
				dotHolder.transform.GetChild( dotHolder.transform.childCount-1 ).transform.SetSiblingIndex(0);
				scrollRect.horizontalNormalizedPosition += GetScrollStep();
			}
			
			// Scroll
			StartCoroutine( AnimateScrollCoroutine(direction) );
			
			// Scale current down
			dots[selectedIndex].GetComponent<CarouselSilderDot>().circle.gameObject.SetActive(false);
			dots[selectedIndex].GetComponent<CarouselSilderDot>().circle.localScale = Vector3.zero;
			StartCoroutine( 
				AnimationController.instance.AnimateScaleCoroutine(
				dots[selectedIndex].GetComponent<CarouselSilderDot>().root,
				dots[selectedIndex].GetComponent<CarouselSilderDot>().root.localScale,
				Vector3.one * (1-1*scaleStep ),
				swipeTime,
				"smooth"
				));
				
			// Scale element at other end of scroll rect down
			int childIndexDotScaleDown = GetScaleDownElementIndex(direction);		
			StartCoroutine( 
				AnimationController.instance.AnimateScaleCoroutine(
				dotHolder.transform.GetChild( childIndexDotScaleDown ).GetComponent<CarouselSilderDot>().root,
				dotHolder.transform.GetChild( childIndexDotScaleDown ).GetComponent<CarouselSilderDot>().root.localScale,
				Vector3.zero,
				swipeTime,
				"smooth"
				));
				
			// Scale element at other end of scroll rect up
			int childIndexDotScaleUp = GetScaleUpElementIndex( direction );			
			StartCoroutine( 
				AnimationController.instance.AnimateScaleCoroutine(
				dotHolder.transform.GetChild( childIndexDotScaleUp ).GetComponent<CarouselSilderDot>().root,
				Vector3.zero,
				Vector3.one * (1-1*scaleStep ),
				swipeTime,
				"smooth"
				));
				
			// Change selected indx
			selectedIndex += direction;
			if( selectedIndex >= dots.Count ) selectedIndex = 0;
			if( selectedIndex < 0 ) selectedIndex = dots.Count - 1;
			
			// Scale new selected up
			dots[selectedIndex].GetComponent<CarouselSilderDot>().circle.gameObject.SetActive(true);
			dots[selectedIndex].GetComponent<CarouselSilderDot>().circle.localScale = Vector3.one;
			yield return StartCoroutine( 
				AnimationController.instance.AnimateScaleCoroutine(
				dots[selectedIndex].GetComponent<CarouselSilderDot>().root,
				dots[selectedIndex].GetComponent<CarouselSilderDot>().root.localScale,
				Vector3.one,
				swipeTime,
				"smooth"
				));
				
			// Reset scale of element at other end of scroll rect
			dotHolder.transform.GetChild( childIndexDotScaleDown ).GetComponent<CarouselSilderDot>().root.localScale = Vector3.one * (1-1*scaleStep );

				
		}else if( direction == 1 && move ){
			
			if( GetDotById( selectedIndex ).transform.GetSiblingIndex() == dotHolder.transform.childCount-2 ){
				dotHolder.transform.GetChild( 0 ).transform.SetSiblingIndex( dotHolder.transform.childCount-1 );
				scrollRect.horizontalNormalizedPosition -= GetScrollStep();
			}
			
			// Scroll
			StartCoroutine( AnimateScrollCoroutine(direction) );
			
			// Scale
			dots[selectedIndex].GetComponent<CarouselSilderDot>().circle.gameObject.SetActive(false);
			dots[selectedIndex].GetComponent<CarouselSilderDot>().circle.localScale = Vector3.zero;
			StartCoroutine( 
				AnimationController.instance.AnimateScaleCoroutine(
				dots[selectedIndex].GetComponent<CarouselSilderDot>().root,
				dots[selectedIndex].GetComponent<CarouselSilderDot>().root.localScale,
				Vector3.one * (1-1*scaleStep ),
				swipeTime,
				"smooth"
				));
				
			// Scale element at other end of scroll rect down
			int childIndexDotScaleDown = GetScaleDownElementIndex( direction );		
			StartCoroutine( 
				AnimationController.instance.AnimateScaleCoroutine(
				dotHolder.transform.GetChild( childIndexDotScaleDown ).GetComponent<CarouselSilderDot>().root,
				dotHolder.transform.GetChild( childIndexDotScaleDown ).GetComponent<CarouselSilderDot>().root.localScale,
				Vector3.zero,
				swipeTime,
				"smooth"
				));
				
			// Scale element at other end of scroll rect up
			int childIndexDotScaleUp = GetScaleUpElementIndex( direction );			
			StartCoroutine( 
				AnimationController.instance.AnimateScaleCoroutine(
				dotHolder.transform.GetChild( childIndexDotScaleUp ).GetComponent<CarouselSilderDot>().root,
				Vector3.zero,
				Vector3.one * (1-1*scaleStep ),
				swipeTime,
				"smooth"
				));
				
			selectedIndex += direction;
			if( selectedIndex >= dots.Count ) selectedIndex = 0;
			if( selectedIndex < 0 ) selectedIndex = dots.Count - 1;
			
			dots[selectedIndex].GetComponent<CarouselSilderDot>().circle.gameObject.SetActive(true);
			dots[selectedIndex].GetComponent<CarouselSilderDot>().circle.localScale = Vector3.one;
			yield return StartCoroutine( 
				AnimationController.instance.AnimateScaleCoroutine(
				dots[selectedIndex].GetComponent<CarouselSilderDot>().root,
				dots[selectedIndex].GetComponent<CarouselSilderDot>().root.localScale,
				Vector3.one,
				swipeTime,
				"smooth"
				));
				
			// Reset scale of element at other end of scroll rect
			dotHolder.transform.GetChild( childIndexDotScaleDown ).GetComponent<CarouselSilderDot>().root.localScale = Vector3.one * (1-1*scaleStep );
			
		}else{
			
			if( currentVisibleIndex != -1 ){
				currentVisibleIndex += direction;
			}
			
			dots[selectedIndex].GetComponent<CarouselSilderDot>().circle.gameObject.SetActive(false);
			dots[selectedIndex].GetComponent<CarouselSilderDot>().circle.localScale = Vector3.zero;
			StartCoroutine( 
				AnimationController.instance.AnimateScaleCoroutine(
				dots[selectedIndex].GetComponent<CarouselSilderDot>().root,
				dots[selectedIndex].GetComponent<CarouselSilderDot>().root.localScale,
				Vector3.one * (1-1*scaleStep ),
				swipeTime,
				"smooth"
				));
				
			selectedIndex += direction;
			if( selectedIndex >= dots.Count ) selectedIndex = 0;
			if( selectedIndex < 0 ) selectedIndex = dots.Count - 1;
				
			dots[selectedIndex].GetComponent<CarouselSilderDot>().circle.gameObject.SetActive(true);
			dots[selectedIndex].GetComponent<CarouselSilderDot>().circle.localScale = Vector3.one;
			yield return StartCoroutine( 
				AnimationController.instance.AnimateScaleCoroutine(
				dots[selectedIndex].GetComponent<CarouselSilderDot>().root,
				dots[selectedIndex].GetComponent<CarouselSilderDot>().root.localScale,
				Vector3.one,
				swipeTime,
				"smooth"
				));
		}
		
		isSwiping = false;
	}
	
	public int GetScaleDownElementIndex( int direction ){
		
		int step = visibleElements - 2;
		int currentSiblingIndex = GetDotById( selectedIndex ).transform.GetSiblingIndex();
		int index = 0;
		
		if( direction == 1 ){
			index = currentSiblingIndex - step;
		}
		else{
			index = currentSiblingIndex + step;
		}
	
		return index;
	}
	
	public int GetScaleUpElementIndex( int direction ){
		
		int currentSiblingIndex = GetDotById( selectedIndex ).transform.GetSiblingIndex();
		int index = 0;
		
		if( direction == 1 ){
			index = currentSiblingIndex + 2;
		}
		else{
			index = currentSiblingIndex - 2;
		}
	
		return index;
	}
	
	public IEnumerator AnimateScrollCoroutine(int direction){
		
		float startScroll = scrollRect.horizontalNormalizedPosition;
		float targetScroll = scrollRect.horizontalNormalizedPosition+direction*GetScrollStep();
		AnimationCurve animationCurve = AnimationController.instance.GetAnimationCurveWithID("smooth");
		if(animationCurve == null) yield break;
		float currentTime = 0;
		while( currentTime < swipeTime ){
			
			float lerpValue = animationCurve.Evaluate( currentTime / swipeTime );
			scrollRect.horizontalNormalizedPosition = Mathf.LerpUnclamped( startScroll, targetScroll, lerpValue );

			currentTime += Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}		
		scrollRect.horizontalNormalizedPosition = targetScroll;
	}
}
