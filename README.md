# KazusaGI_cb2
 Server sofware reimplementaion for an old version of a certain anime game

## Please note: the project is still WIP and is in its very early stages, and many features might not work as intended. Same with the code quality, it will go through many rewrites and refactoring in the future, so please don't judge :\)

# Usage
- Download [MongoDB 4.0+](https://www.mongodb.com/try/download/community)
- Compile via Visual Studio 2022
- Download `BinOutput`, `Lua`, `ExcelBinOutput` and `ServerExcelOutput` from [the resources repo](https://github.com/Hiro420/KazusaGI_Data) and place them in `resources` folder, next to the executable
- Download [0.7.1](https://autopatchhk.yuanshen.com/client_app/pc_plus19/Genshin_0.7.1.zip) or [0.7.0](https://autopatchhk.yuanshen.com/client_app/pc_plus19/Genshin_0.7.0.zip) version of a certain anime game 
- Place a patched [mhyprot2.Sys](https://drive.google.com/file/d/13JllxRsyZSb4rfATNJXNoKHWApcpD7Kn/view?usp=drive_link) in the game folder, next to the executable
- [Download](https://api.getfiddler.com/fc/latest) and [configure](#Configure-Fiddler) Fiddler
- Profit

# Configure Fiddler
### To access options, you go to Tools->Options
## HTTPS Section
- Capture HTTPS CONNECTs -> enable
(if it asks you if you want to install Fiddler certificate, select Yes)
- Decrypt HTTPS traffic -> enable
- Ignore server certificate errors -> enable
## Connections Section
- Port -> 9999
- Capture FTP requests -> enable
## Gateway Section
- Either `Use System Proxy` or `Automatically Detect Proxy using WPAD`
### To access FiddlerScript, you go to the FiddlerScript tab
- Delete the contents and past the following scipt:
```js
import System;
import System.Windows.Forms;
import Fiddler;
import System.Text.RegularExpressions;
var list = [
    "https://api-os-takumi.mihoyo.com/",
    "https://hk4e-api-os-static.mihoyo.com/",
    "https://hk4e-sdk-os.mihoyo.com/",
    "https://dispatchosglobal.yuanshen.com/",
    "https://osusadispatch.yuanshen.com/",
    "https://account.mihoyo.com/",
    "https://log-upload-os.mihoyo.com/",
    "https://dispatchcntest.yuanshen.com/",
    "https://devlog-upload.mihoyo.com/",
    "https://webstatic.mihoyo.com/",
    "https://log-upload.mihoyo.com/",
    "https://hk4e-sdk.mihoyo.com/",
    "https://api-beta-sdk.mihoyo.com/",
    "https://api-beta-sdk-os.mihoyo.com/",
    "https://cnbeta01dispatch.yuanshen.com/",
    "https://dispatchcnglobal.yuanshen.com/",
    "https://cnbeta02dispatch.yuanshen.com/",
    "https://sdk-os-static.mihoyo.com/",
    "https://webstatic-sea.mihoyo.com/",
    "https://webstatic-sea.hoyoverse.com/",
    "https://hk4e-sdk-os-static.hoyoverse.com/",
    "https://sdk-os-static.hoyoverse.com/",
    "http://dispatch.osglobal.yuanshen.com",
    "https://sandbox-sdk.mihoyo.com/",
    "https://dispatch.osglobal.yuanshen.com/",
    "https://hk4e-sdk-os.hoyoverse.com/",
    "https://api-sdk.mihoyo.com"// Line 24
    ];
class Handlers{
    static function OnBeforeRequest(oS: Session) {
        var active = true;
        if(active) {
            if(oS.uriContains("http://overseauspider.yuanshen.com:8888/log")){
                oS.oRequest.FailSession(404, "Blocked", "yourmom");
            }
            for(var i = 0; i < 24 ;i++) {
                if(oS.uriContains(list[i])) {
                    oS.host = "127.0.0.1";
                    oS.oRequest.headers.UriScheme = "http";
                    oS.port = 3000; // This can also be replaced with another IP address.
                }
            }
        }
    }
};
```
- Press `Save Script`, a sound indicating success should be heard.

### Personal TODO List
- fix all abilities to be tracked properly
- improve ScriptLib (complex stuff like challenges)
- StartPlatform, StopPlatform
- tower (Spiral Abyss)
- NOT load quest-related lua groups
- add relics
