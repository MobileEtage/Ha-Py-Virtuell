
using Mapbox.Utils;
using UnityEngine;

public static class Params
{
	// MapFilterController
	public static bool showMapFilterOptions = true;

	// ARController
	public static bool alwaysResetARSession = true;

	// FeedbackController
	public static string feedbackURL = "https://app-etagen.die-etagen.de/Stadtrundgang/Hameln/SendMail/sendMail.php";

	// DestinationOneController, MapFilterController
	public static string destinationOneExperience = "";
    public static string destinationOneEventExperience = "";

    // DestinationOneController
    public static string hotelLink = "";

    // MenuController
    public static string newsletterLink = "https://www.ar-guide.de/";

    // MapController
    public static bool showPathButton = false;
	public static Vector2d mapCenter = new Vector2d( 52.167794192952d, 9.43903970937649d );					// Glashütte am Kleinen Süntel
	public static Vector2d mapPersonTestPosition = new Vector2d(52.167721035165954d, 9.439068065302642d);
    public static float mapZoom = 17.875f;
    public static Color pathButtonImageColorActive;
    public static Color pathButtonImageColorInActive;
    public static Color mapStationFinishedLabelColor;
    public static Color mapStationNotFinishedLabelColor;

    // MapFilterController
    public static Vector2d mapSearchCenter = new Vector2d(52.10335364644705d, 9.363723008932705d);	// Hameln Mitte
    public static bool showMapFilters = false;
    public static Color mapMenuToursBackgroundColor;
    public static Color mapMenuFiltersBackgroundColor;

    // TourListElement
    public static Color mapMenuStationsLabelColor;

    // MapStation
    public static Color mapStationNotFinishedBackgroundColor;

    // GuideController
    public static Color guideMenuButtonActiveColor;

    // ARMenuController
    public static Color arMenuButtonActivColor;
    public static Color arMenuButtonActivColor2;
    public static Color arMenuIconsInActivColor;

    // StationController
	public static string eventsWebLink = "";
    public static Color stationListElementBackgroundColorFinished;
    public static Color stationListElementBackgroundColorNotFinished;
    public static Color stationListElementNumberLabelColorFinished;
    public static Color stationListElementNumberLabelColorNotFinished;
    public static Color stationListElementTitleLabelColorFinished;
    public static Color stationListElementTitleLabelColorNotFinished;

    // VideoController
    public static bool highlightMenuAfterVideo = true;
    public static bool showInfoSiteAfterVideo = false;
    public static float sideBySideVideoScale = 4.2f;
    public static float greenscreenVideoScale = 4.7f;
    public static float sideBySideVideoYOffset = 0.0f;
    public static float greenscreenVideoYOffset = -0.35f;

    // DownloadContentController
    public static bool showIntroVideo = false;
	public static bool showTutorial = false;

	// PhotoController
	public static string galleryAlbum = "Hameln";

    // PermissionController
    public static bool supportAllAndroidDevices = true;

    // FirebaseController
    // MenuController
    public static bool usePrivacyWeblink = false;
    public static string privacyURL = "https://www.expo-etage.de/datenschutz/";

    // MenuController
    public static bool useImprintWeblink = false;
    public static string imprintURL = "https://www.expo-etage.de/impressum/";

    static Params()
	{
        guideMenuButtonActiveColor = GetColorFromHexString( "005CA9" );
        arMenuButtonActivColor = GetColorFromHexString( "005CA9" );
        arMenuButtonActivColor2 = GetColorFromHexString( "005CA9" );
        arMenuIconsInActivColor = GetColorFromHexString( "181919" );

        stationListElementBackgroundColorFinished = GetColorFromHexString( "002E57" );
        stationListElementBackgroundColorNotFinished = GetColorFromHexString( "002E57" );
        stationListElementNumberLabelColorFinished = GetColorFromHexString( "FFFFFF88" );
        stationListElementNumberLabelColorNotFinished = GetColorFromHexString( "FFFFFF88" );
        stationListElementTitleLabelColorFinished = GetColorFromHexString( "F7F7F7" );
        stationListElementTitleLabelColorNotFinished = GetColorFromHexString( "F7F7F7" );

        pathButtonImageColorActive = GetColorFromHexString("6CB931");
        pathButtonImageColorInActive = GetColorFromHexString("212121");

        mapStationFinishedLabelColor = GetColorFromHexString( "005CA9" );
        mapStationNotFinishedLabelColor = GetColorFromHexString("FFFFFF");
        mapStationNotFinishedBackgroundColor = GetColorFromHexString( "005CA9" );

        mapMenuToursBackgroundColor = GetColorFromHexString( "002E57" );
        mapMenuFiltersBackgroundColor = GetColorFromHexString( "002E57" );
        mapMenuStationsLabelColor = GetColorFromHexString("FFFFFF40");

    }

    public static Color GetColorFromHexString( string hexCode ){
		
		if( hexCode.Length == 6 ){	// avoid transparent color
			hexCode += "FF";
		}
		
		if( !hexCode.StartsWith("#") ){
			hexCode = "#" + hexCode;
		}
		
		Color color;
		if (ColorUtility.TryParseHtmlString(hexCode, out color))
			return color;
			
		return new Color( 51f/255f, 63f/255f, 72f/255f );	// Tetra color
	}
}
