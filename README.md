# Virtual ZPL Printer
An Ethernet based virtual Zebra Label Printer that can be used to test applications that produce bar code labels. This application uses the Labelary service found at [http://labelary.com](http://labelary.com/service.html).

## Latest Release
[Download the installer](https://github.com/porrey/Virtual-ZPL-Printer/raw/main/Installer/Virtual%20ZPL%20Printer%20Setup.msi) (v 3.1.0)

Now requires **.NET 8.0**. Download Setup.exe and the MSI if you need to have the .NET 8.0 Framework installed automatically.

###### Version 3.1.0 Updates:
1. Exposed the Labelary API URL in global settings to allow HTTPS or HTTP.
2. Added the option to use POST or GET in global settings.
3. Added an option to enable Linting in global settings. Linting is an option provided by Labelary to provide warnings for the ZPL text sent to the API.
4. Added a viewer for the ZPL warnings. The viewer can be accesses via the context menu on an image or by clicking the button on the label image (which is only visible when warnings exist).
5. Added a preview button on the label image to open the image viewer.
6. Made the connection test viewer output read-only.
 
## Screen Shots

![](https://github.com/porrey/Virtual-ZPL-Printer/raw/main/Images/VirtualZplPrinter-01.png)

![](https://github.com/porrey/Virtual-ZPL-Printer/raw/main/Images/VirtualZplPrinter-02.png)

![](https://github.com/porrey/Virtual-ZPL-Printer/raw/main/Images/VirtualZplPrinter-03.png)

![](https://github.com/porrey/Virtual-ZPL-Printer/raw/main/Images/VirtualZplPrinter-04.png)

![](https://github.com/porrey/Virtual-ZPL-Printer/raw/main/Images/VirtualZplPrinter-05.png)

## History
###### Version 3.0.2 Updates:
1. Added menu option to test Labelary connectivity.
2. Added the ability to configure a physical printer to print a label retrieved from the Labelary API. This is currently limited in functionality and may be expanded in future versions.

###### Version 3.0.1 Updates:
1. Added support to specify the cached image file name in ZPL using a comment such as `^FX ImageFileName: USPS-Shipping`. The file numbering is still controlled by the application (as well as page numbering when a ZPL file contains more than one label).

###### Version 3.0.0 Updates:
1. Updated to .NET 8.0
2. Merged Pull-Request: Avoid splitting large messages | Make encoding configurable #37
2. Fixed Bug: Sizes in millimeters or centimeters not saving correctly #29
3. Fixed Bug: Test windows not working properly (can't paste or add enters) #30
4. Fixed Bug: Print settings takes into account regional formatting #33
5. Fixed Bug: Print ZPL Bug #38

###### Version 2.3.0 Updates:
1. Fixed issue with test label window not reopening.
2. Added ZPL editor to test label window (no save option).
3. Added application menu.
4. Added hot keys for certain actions.
5. Added global settings (with configurable TCP settings).
6. Added about menu.

###### Version 2.2.0 Updates:
1. Made IP address editable.
2. Corrected an issue where the configuration dialog would only open once.

###### Version 2.1.0 Updates:
1. Upgraded to .NET 7 framework.
2. Added ZPL filters. One or more find replace filters (supporting regular expressions) can be added to each printer configuration.

###### Version 2.0.2 Updates:
1. Large labels (greater than 8192 bytes) were being truncated. Changed TCP/IP receive buffer size to be dynamic.

###### Version 2.0.1 Updates:
1. Re-factored code/cleanup.
2. Added USPS sample label.
3. Corrected an issue where too many labels at one time could cause a multi-thread related exception.

###### Version 2.0.0 Updates:
1. Re-factored user interface.
2. Added printer configurations.
3. Added test label templates. Custom templates can be added.

###### Version 1.0.18 Updates:
1. Use Labelary `X-Total-Count` HTTP header to determine the number of labels to create.

###### Version 1.0.17 Updates:
1. Added rotation support.
2. Added support for multiple labels within a single ZPL document.
