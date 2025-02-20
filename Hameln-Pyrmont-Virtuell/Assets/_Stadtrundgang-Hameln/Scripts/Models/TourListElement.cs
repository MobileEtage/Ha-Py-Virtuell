using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SimpleJSON;

public class TourListElement : MonoBehaviour
{
    public string tourId = "";
    public string stationId = "";
    public Image previewImage;
    public TextMeshProUGUI titleLabel;
    public TextMeshProUGUI stationsLabel;
    public TextMeshProUGUI numberLabel;
    public Image background;
    public GameObject selection;

    public GameObject tourInfo;
    public TextMeshProUGUI distanceLabel;
    public TextMeshProUGUI durationLabel;
	public bool updateHeight = false;
	public bool isUpdated = false;

	private void Start()
    {
        if (numberLabel != null) { numberLabel.color = Params.mapMenuStationsLabelColor; }
    }

	public void OnEnable()
	{
		if ( isUpdated ) return;
		if ( !updateHeight ) return;
		if ( previewImage.sprite == null ) return;

		UpdateHeight();
	}

	public void UpdateHeight()
	{
		if ( isUpdated ) return;
		if ( !updateHeight ) return;
		if ( previewImage.sprite == null ) return;

		isUpdated = true;
		StartCoroutine(UpdateHeightCoroutine());
	}

	public IEnumerator UpdateHeightCoroutine()
	{
		yield return null;
		SetHeight();
	}

	public void SetHeight()
	{
		float ratio = previewImage.sprite.bounds.size.y / previewImage.sprite.bounds.size.x;
		float w = transform.GetChild( 0 ).GetComponent<RectTransform>().rect.width;
		float h = ratio * w;
		previewImage.GetComponentInParent<LayoutElement>( true ).preferredHeight = h;
	}
}
