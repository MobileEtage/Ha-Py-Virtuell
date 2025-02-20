

#import "iOSToolsController.h"
#import <Photos/Photos.h>
#import <CoreLocation/CoreLocation.h>

static iOSToolsController* toolsController;

@implementation iOSToolsController

CLLocationManager *_locationManager;

- (char *) cStringCopy: (const char*) string;
{
    if (string == NULL)
        return NULL;
    
    char* res = (char*)malloc(strlen(string) + 1);
    strcpy(res, string);
    
    return res;
}

- (void) ForwardToAppSettings
{
    [[UIApplication sharedApplication] openURL:[NSURL URLWithString:UIApplicationOpenSettingsURLString]];
}

- (void) ForwardToLocationSettings
{
    //[[UIApplication sharedApplication] openURL:[NSURL URLWithString:@"Prefs:root=LOCATION_SERVICES"]];
    //[[UIApplication sharedApplication] openURL:[NSURL URLWithString:@"App-Prefs:root=LOCATION_SERVICES"]];
    //[[UIApplication sharedApplication] openURL:[NSURL URLWithString:@"App-prefs:root=LOCATION_SERVICES"]];

    //NSURL *URL = [NSURL URLWithString:@"App-prefs:root=LOCATION_SERVICES"];
    //NSURL *URL = [NSURL URLWithString:@"App-prefs:root=Privacy"];
    //NSURL *URL = [NSURL URLWithString:@"App-prefs:root=Privacy&path=LOCATION"];
    //NSURL *URL = [NSURL URLWithString:@"App-prefs:root=Privacy&Security&path=LOCATION"];
    NSURL *URL = [NSURL URLWithString:@"App-prefs:root=Privacy&Security&path=LOCATION"];
    [[UIApplication sharedApplication] openURL:URL options:@{} completionHandler:nil];
}

- (void) requestPermissionSettings:(NSString *)NSGameObject withTitle:(NSString *)title withMessage:(NSString *)message withOkButton:(NSString *)okButton withAbortButton:(NSString *)abortButton withCallback:(NSString *)NSCallback
{
    UIAlertController *alert = [UIAlertController alertControllerWithTitle:title message:message preferredStyle:UIAlertControllerStyleAlert];
      
    UIAlertAction *ok = [UIAlertAction actionWithTitle:okButton style:UIAlertActionStyleDefault handler:^(UIAlertAction * _Nonnull action)
    {
      [[UIApplication sharedApplication] openURL:[NSURL URLWithString:UIApplicationOpenSettingsURLString]];
    }];

    UIAlertAction *cancel = [UIAlertAction actionWithTitle:abortButton style:UIAlertActionStyleDefault handler:^(UIAlertAction * _Nonnull action)
    {
    UnitySendMessage(([NSGameObject cStringUsingEncoding:NSUTF8StringEncoding]),
                    ([NSCallback cStringUsingEncoding:NSUTF8StringEncoding]), "false");
    }];

    [alert addAction:cancel];
    [alert addAction:ok];

    UIViewController *viewController = [[[[UIApplication sharedApplication] delegate] window] rootViewController];
    if ( viewController.presentedViewController && !viewController.presentedViewController.isBeingDismissed ) {
      viewController = viewController.presentedViewController;
    }

    NSLayoutConstraint *constraint = [NSLayoutConstraint
                                    constraintWithItem:alert.view
                                    attribute:NSLayoutAttributeHeight
                                    relatedBy:NSLayoutRelationLessThanOrEqual
                                    toItem:nil
                                    attribute:NSLayoutAttributeNotAnAttribute
                                    multiplier:1
                                    constant:viewController.view.frame.size.height*2.0f];

    [alert.view addConstraint:constraint];
    [viewController presentViewController:alert animated:YES completion:^{}];

}

- (void) RequestCameraPermission
{
    // Request permission to access the camera and microphone.
    switch ([AVCaptureDevice authorizationStatusForMediaType:AVMediaTypeVideo])
    {
        case AVAuthorizationStatusAuthorized:
        {
            UnitySendMessage("PermissionController", "OnCameraPermissionRequestCompleted", "true");
            break;
        }
        case AVAuthorizationStatusNotDetermined:
        {
            // The app hasn't yet asked the user for camera access.
            [AVCaptureDevice requestAccessForMediaType:AVMediaTypeVideo completionHandler:^(BOOL granted) {
                if (granted) {
                    UnitySendMessage("PermissionController", "OnCameraPermissionRequestCompleted", "true");
                }else{
                    UnitySendMessage("PermissionController", "OnCameraPermissionRequestCompleted", "false");
                }
            }];
            break;
        }
        case AVAuthorizationStatusDenied:
        {
            UnitySendMessage("PermissionController", "OnCameraPermissionRequestCompleted", "false");
        }
        case AVAuthorizationStatusRestricted:
        {
            UnitySendMessage("PermissionController", "OnCameraPermissionRequestCompleted", "false");
        }
    }
}

- (void) RequestLocationPermission
{
    NSLog(@"RequestLocationPermission");

    //CLLocationManager *locationManager = [[CLLocationManager alloc] init];
    _locationManager = [[CLLocationManager alloc] init];
    
    //[_locationManager requestAlwaysAuthorization];
    //[_locationManager requestWhenInUseAuthorization];
    
    _locationManager = [[CLLocationManager alloc] init];
    if ([_locationManager respondsToSelector:@selector(requestAlwaysAuthorization)]) {
        [_locationManager requestAlwaysAuthorization];
    }
}

- (void)locationManager:(CLLocationManager *)manager didChangeAuthorizationStatus:(CLAuthorizationStatus)status {

    NSLog(@"locationManager didChangeAuthorizationStatus");
    
    switch (status)
    {
        case kCLAuthorizationStatusAuthorizedAlways:
        {
            UnitySendMessage("PermissionController", "OnLocationPermissionRequestCompleted", "true");
            break;
        }
        case kCLAuthorizationStatusAuthorizedWhenInUse:
        {
            UnitySendMessage("PermissionController", "OnLocationPermissionRequestCompleted", "true");
            break;
        }
        case kCLAuthorizationStatusNotDetermined:
        {
            UnitySendMessage("PermissionController", "OnLocationPermissionRequestCompleted", "false");
            break;
        }
        case kCLAuthorizationStatusDenied:
        {
            UnitySendMessage("PermissionController", "OnLocationPermissionRequestCompleted", "false");
            break;
        }
        case kCLAuthorizationStatusRestricted:
        {
            UnitySendMessage("PermissionController", "OnLocationPermissionRequestCompleted", "false");
            break;
        }
    }
}

- (NSString*) HasCameraPermission
{
    switch ([AVCaptureDevice authorizationStatusForMediaType:AVMediaTypeVideo])
    {
        case AVAuthorizationStatusAuthorized:
        {
            return @"true";
            break;
        }
        case AVAuthorizationStatusNotDetermined:
        {
            //return @"false";
            return @"AVAuthorizationStatusNotDetermined";
            break;
        }
        case AVAuthorizationStatusDenied:
        {
            return @"false";
            break;
        }
        case AVAuthorizationStatusRestricted:
        {
            return @"false";
            break;
        }
    }
    return @"false";
}

- (NSString*) HasMicrophonePermission
{
    switch ([AVCaptureDevice authorizationStatusForMediaType:AVMediaTypeAudio])
    {
        case AVAuthorizationStatusAuthorized:
        {
            return @"true";
            break;
        }
        case AVAuthorizationStatusNotDetermined:
        {
            //return @"false";
            return @"AVAuthorizationStatusNotDetermined";
            break;
        }
        case AVAuthorizationStatusDenied:
        {
            return @"false";
            break;
        }
        case AVAuthorizationStatusRestricted:
        {
            return @"false";
            break;
        }
    }
    return @"false";
}

- (NSString*) HasLocationPermission
{
    NSLog(@"HasLocationPermission");
    
    if ([CLLocationManager locationServicesEnabled]){

        NSLog(@"Location Services Enabled");
        
        switch ([CLLocationManager authorizationStatus])
        {
            case kCLAuthorizationStatusAuthorizedAlways:
            {
                NSLog(@"Location Services kCLAuthorizationStatusAuthorizedAlways");
                return @"true";
                break;
            }
            case kCLAuthorizationStatusAuthorizedWhenInUse:
            {
                NSLog(@"Location Services kCLAuthorizationStatusAuthorizedWhenInUse");
                return @"true";
                break;
            }
            case kCLAuthorizationStatusNotDetermined:
            {
                NSLog(@"Location Services kCLAuthorizationStatusNotDetermined");
                return @"kCLAuthorizationStatusNotDetermined";
                break;
            }
            case kCLAuthorizationStatusDenied:
            {
                NSLog(@"Location Services kCLAuthorizationStatusDenied");
                return @"false";
                break;
            }
            case kCLAuthorizationStatusRestricted:
            {
                NSLog(@"Location Services kCLAuthorizationStatusRestricted");
                return @"false";
                break;
            }
        }
    }
    else{
        return @"false";
    }

    return @"false";
}

- (NSString*) LocationServicesEnabled
{
    if ([CLLocationManager locationServicesEnabled]){
        return @"true";
    }
    else{
        return @"false";
    }
    return @"false";
}

/*
- (void) requestPermissionCamera
{
    [AVCaptureDevice requestAccessForMediaType:AVMediaTypeVideo completionHandler:^(BOOL granted) {
        if (granted) {
            
        }
    }];
}
 */

@end

extern "C" {
    
    void ForwardToAppSettings()
    {
        if( toolsController == nil ){
            toolsController = [[iOSToolsController alloc] init];
        }
        
        [toolsController ForwardToAppSettings];
    }

    void ForwardToLocationSettings()
    {
        if( toolsController == nil ){
            toolsController = [[iOSToolsController alloc] init];
        }
        
        [toolsController ForwardToLocationSettings];
    }
    
    extern char* HasCameraPermission()
    {
        if( toolsController == nil ){
            toolsController = [[iOSToolsController alloc] init];
        }
        
        NSString *granted = [toolsController HasCameraPermission];
        return [toolsController cStringCopy:[granted UTF8String]];
    }

    extern char* HasMicrophonePermission()
    {
        if( toolsController == nil ){
            toolsController = [[iOSToolsController alloc] init];
        }
        
        NSString *granted = [toolsController HasMicrophonePermission];
        return [toolsController cStringCopy:[granted UTF8String]];
    }

    extern char* HasLocationPermission()
    {
        if( toolsController == nil ){
            toolsController = [[iOSToolsController alloc] init];
        }
        
        NSString *granted = [toolsController HasLocationPermission];
        return [toolsController cStringCopy:[granted UTF8String]];
    }

    extern char* LocationServicesEnabled()
    {
        if( toolsController == nil ){
            toolsController = [[iOSToolsController alloc] init];
        }
        
        NSString *granted = [toolsController LocationServicesEnabled];
        return [toolsController cStringCopy:[granted UTF8String]];
    }

    void RequestCameraPermission()
    {
        if( toolsController == nil ){
            toolsController = [[iOSToolsController alloc] init];
        }
        
        return [toolsController RequestCameraPermission];
    }

    void RequestLocationPermission()
    {
        if( toolsController == nil ){
            toolsController = [[iOSToolsController alloc] init];
        }
        
        return [toolsController RequestLocationPermission];
    }

    float GetStatusBarHeight()
    {
        float statusBarPointHeight = [UIApplication sharedApplication].statusBarFrame.size.height;
        CGFloat scale = [UIScreen mainScreen].scale;
        return statusBarPointHeight * scale;
    }

    char* GetCountryCode()
    {
        if( toolsController == nil ){
            toolsController = [[iOSToolsController alloc] init];
        }
        
        NSLocale *currentLocale = [NSLocale currentLocale];  // get the current locale.
        NSString *countryCode = [currentLocale objectForKey:NSLocaleCountryCode]; // get country code, e.g. ES (Spain), FR (France), etc.
        return [toolsController cStringCopy:[countryCode UTF8String]];
    }

    extern void RequestPermissionSettings(const char* gameObject, const char* title, const char* message, const char* okButton, const char* abortButton, const char* callback)
    {
        if( toolsController == nil ){
            toolsController = [[iOSToolsController alloc] init];
        }

        NSString *NSGameObject = [[NSString alloc] initWithUTF8String:gameObject];
        NSString *NSTitle = [[NSString alloc] initWithUTF8String:title];
        NSString *NSMessage = [[NSString alloc] initWithUTF8String:message];
        NSString *NSOkButton = [[NSString alloc] initWithUTF8String:okButton];
        NSString *NSAbortButton = [[NSString alloc] initWithUTF8String:abortButton];
        NSString *NSCallback = [[NSString alloc] initWithUTF8String:callback];

        [toolsController requestPermissionSettings:NSGameObject withTitle:NSTitle withMessage: NSMessage withOkButton: NSOkButton withAbortButton: NSAbortButton withCallback:NSCallback];
    }
    
    extern char* GetPhotoLibraryAuthorizationStatus()
    {
        if( toolsController == nil ){
            toolsController = [[iOSToolsController alloc] init];
        }
        
        PHAuthorizationStatus status = [PHPhotoLibrary authorizationStatus];

        if (status == PHAuthorizationStatusAuthorized) {
            // Access has been granted.
            return [toolsController cStringCopy:[@"PHAuthorizationStatusAuthorized" UTF8String]];
        }
        
        else if (status == PHAuthorizationStatusDenied) {
            // Access has been denied.
            return [toolsController cStringCopy:[@"PHAuthorizationStatusDenied" UTF8String]];
        }
        
        else if (status == PHAuthorizationStatusNotDetermined) {
            
            // Access has not been determined.
            /*
            [PHPhotoLibrary requestAuthorization:^(PHAuthorizationStatus status) {
                
                if (status == PHAuthorizationStatusAuthorized) {
                    // Access has been granted.
                    //UnitySendMessage(([NSGameObject cStringUsingEncoding:NSUTF8StringEncoding]),
                    //                 ([NSCallback cStringUsingEncoding:NSUTF8StringEncoding]), "false");
                }
                
                else {
                    // Access has been denied.
                }
            }];
            */
            return [toolsController cStringCopy:[@"PHAuthorizationStatusNotDetermined" UTF8String]];
        }
        
        else if (status == PHAuthorizationStatusRestricted) {
            // Restricted access - normally won't happen.
            return [toolsController cStringCopy:[@"PHAuthorizationStatusRestricted" UTF8String]];
        }
    }
    
    extern void RequestPermissionPhotoLibrary(const char* gameObject, const char* callback)
    {
        if( toolsController == nil ){
            toolsController = [[iOSToolsController alloc] init];
        }
        
        NSString *NSGameObject = [[NSString alloc] initWithUTF8String:gameObject];
        NSString *NSCallback = [[NSString alloc] initWithUTF8String:callback];
        
        [PHPhotoLibrary requestAuthorization:^(PHAuthorizationStatus status) {
            
            if (status == PHAuthorizationStatusAuthorized) {
                // Access has been granted.
                UnitySendMessage(([NSGameObject cStringUsingEncoding:NSUTF8StringEncoding]),
                                 ([NSCallback cStringUsingEncoding:NSUTF8StringEncoding]), "true");
            }
            
            else {
                // Access has been denied.
                UnitySendMessage(([NSGameObject cStringUsingEncoding:NSUTF8StringEncoding]),
                                 ([NSCallback cStringUsingEncoding:NSUTF8StringEncoding]), "false");
            }
        }];
    }
}


