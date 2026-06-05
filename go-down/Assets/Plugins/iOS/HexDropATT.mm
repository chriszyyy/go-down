// App Tracking Transparency 原生桥接（iOS 14+）。
// 由 AppTrackingTransparencyHelper.cs 通过 DllImport("__Internal") 调用。
// status 取值: 0=NotDetermined, 1=Restricted, 2=Denied, 3=Authorized, -1=不可用(<iOS14)
#import <Foundation/Foundation.h>
#import <AppTrackingTransparency/AppTrackingTransparency.h>
#import <AdSupport/AdSupport.h>

extern "C" {

typedef void (*HexDropATTCallback)(int status);

void HexDrop_RequestTrackingAuthorization(HexDropATTCallback callback) {
    if (@available(iOS 14, *)) {
        [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status) {
            if (callback != NULL) {
                callback((int)status);
            }
        }];
    } else {
        if (callback != NULL) {
            callback(-1);
        }
    }
}

int HexDrop_TrackingAuthorizationStatus() {
    if (@available(iOS 14, *)) {
        return (int)[ATTrackingManager trackingAuthorizationStatus];
    }
    return -1;
}

}
