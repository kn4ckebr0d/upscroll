#import <UIKit/UIKit.h>
#import <AVFoundation/AVFoundation.h>

// Taptic Engine feedback and audio session setup for ScrollyRoad (called from Haptics.cs and Synth.cs).

static UIImpactFeedbackGenerator *ScrollyImpactGenerators[5];
static UINotificationFeedbackGenerator *ScrollyNotifier;

extern "C" {

void ScrollyHapticImpact(int style, float intensity) {
    if (style < 0 || style > 4) style = 0;
    if (ScrollyImpactGenerators[style] == nil) {
        UIImpactFeedbackStyle styles[5] = {
            UIImpactFeedbackStyleLight, UIImpactFeedbackStyleMedium, UIImpactFeedbackStyleHeavy,
            UIImpactFeedbackStyleSoft, UIImpactFeedbackStyleRigid,
        };
        ScrollyImpactGenerators[style] = [[UIImpactFeedbackGenerator alloc] initWithStyle:styles[style]];
    }
    [ScrollyImpactGenerators[style] impactOccurredWithIntensity:intensity];
}

// Alpha: play sound even with the silent switch on (like the Swift prototype), mixing with music.
void ScrollyAudioPlayEvenWhenSilent(void) {
    [[AVAudioSession sharedInstance] setCategory:AVAudioSessionCategoryPlayback
                                     withOptions:AVAudioSessionCategoryOptionMixWithOthers
                                           error:nil];
    [[AVAudioSession sharedInstance] setActive:YES error:nil];
}

void ScrollyHapticNotify(int type) {
    if (ScrollyNotifier == nil) ScrollyNotifier = [[UINotificationFeedbackGenerator alloc] init];
    [ScrollyNotifier notificationOccurred:(UINotificationFeedbackType)type];
}

}
