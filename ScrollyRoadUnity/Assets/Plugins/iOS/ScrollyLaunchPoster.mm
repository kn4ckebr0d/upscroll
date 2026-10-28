#import <UIKit/UIKit.h>

// The launch poster: BuildScript puts it over Unity's view at startup (tag 0x5C0, the same picture as the
// launch screen), so there's no dark gap while the engine loads. The game calls this once the title is drawn.

extern "C" void ScrollyHideLaunchPoster(float delay) {
    dispatch_async(dispatch_get_main_queue(), ^{
        for (UIScene* scene in UIApplication.sharedApplication.connectedScenes) {
            if (![scene isKindOfClass:[UIWindowScene class]]) continue;
            for (UIWindow* window in ((UIWindowScene*)scene).windows) {
                UIView* poster = [window viewWithTag:0x5C0];
                if (poster == nil) continue;
                [UIView animateWithDuration:0.35 delay:delay options:UIViewAnimationOptionCurveEaseOut
                                 animations:^{ poster.alpha = 0; }
                                 completion:^(BOOL finished) { [poster removeFromSuperview]; }];
            }
        }
    });
}
