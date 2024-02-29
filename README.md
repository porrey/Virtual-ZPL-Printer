# Virtual ZPL Printer
An Ethernet based virtual Zebra Label Printer that can be used to test applications that produce bar code labels. This application uses the Labelary service found at [http://labelary.com](http://labelary.com/service.html).

## Latest Release
[Download the installer](https://github.com/porrey/Virtual-ZPL-Printer/raw/main/Installer/Virtual%20ZPL%20Printer%20Setup.msi) (v 3.3.0)

Now requires **.NET 8.0**. Download Setup.exe and the MSI if you need to have the .NET 8.0 Framework installed automatically.

###### Version 3.3.0 Updates:
1. Added multi-language support. Currently added support for Spanish (**es**) and Ukrainian (**uk**). Both translations were done using Google Translate tool and require additional work to be done. These languages can be updated via a pull-request or a new language supported added via same. Right to Left reading languages have not been tested yet. See section on adding, requesting or updating languages. Note that the Labelary API returns messages in English and they are not translated. This language also uses a library called **UnitsNet** that has its own language support. In some cases output from this library may be in English.
2. Moved core ZPL templates to application folder. Custom templates can still be dropped in the personal folder as before.

## Screen Shots

![](https://github.com/porrey/Virtual-ZPL-Printer/raw/main/Images/VirtualZplPrinter-01.png)

![](https://github.com/porrey/Virtual-ZPL-Printer/raw/main/Images/VirtualZplPrinter-02.png)

![](https://github.com/porrey/Virtual-ZPL-Printer/raw/main/Images/VirtualZplPrinter-03.png)

![](https://github.com/porrey/Virtual-ZPL-Printer/raw/main/Images/VirtualZplPrinter-04.png)

![](https://github.com/porrey/Virtual-ZPL-Printer/raw/main/Images/VirtualZplPrinter-05.png)

![](https://github.com/porrey/Virtual-ZPL-Printer/raw/main/Images/VirtualZplPrinter-06.png)

![](https://github.com/porrey/Virtual-ZPL-Printer/raw/main/Images/VirtualZplPrinter-07.png)

![](https://github.com/porrey/Virtual-ZPL-Printer/raw/main/Images/VirtualZplPrinter-08.png)

## Requesting, Adding or Updating Languages
### Requesting a new Language/Region
To request anew language/region add an issue using the label **Add Language**. The language will be added using Google Translate.

### Adding Language/Region Support
Add a pull-request with the title "**Added Language xx-YY**" or "**Added Language xx**". Only the the XML string files should be included in the pull request. If any other files are modified, the pull request will not be accepted. You must ensure that all strings are included in each of the XML files. Ensure the file names are correct: **Strings.xx-YY.resx** or **Strings.xx.resx**.

Issues posted to correct or change a specific word will not be accepted. If you come across a word that should be translated differently, perform a pull-request to change it.

### Updating Language/Region Support
Add a pull-request with the title "**Updated Language xx-YY**" or "**Updated Language xx**". Only the the XML string files should be included in the pull request. If any other files are modified, the pull request will not be accepted. Ensure the files are not renamed. If you are not the original creator of the language file, please seek a review from the original creator. If the original creator does not respond within one week, the pull-request can be accepted without their review.

Issues posted to correct or change a specific word will not be accepted. If you come across a word that should be translated differently, perform a pull-request to change it.

## History
###### Version 3.2.1 Updates:
1. Fixed Issue #51 - Humanizer library caused a crash on unsupported languages. Add exception handling and fallback formatting when language is not supported. Also added all currently supported languages.
2. Updated button icons.

###### Version 3.2.0 Updates:
1. Added the ability to load **custom TrueType fonts** in the printer and use them in the ZPL.
2. Clicking the Test button will bring the window to the front if it is already open.
3. Further re-factored the project structure and code to allow for extended capabilities.

###### Version 3.1.1 Updates:
1. Fixed issue #48.
2. Fixed issue #49.
3. Refactored TCP listener to allow for extended capabilities in future releases. The listener uses request handlers to process incoming requests.
4. Corrected issue preventing last filter from being deleted.
5. Corrected issue where the the application tries to read from the network when there is no more data available. Added a check to `stream.DataAvailable`.
6. Changed `stream.ReadAsync()` to use memory based overloads.
7. Added detailed logging using Serilog. The path defaults to ***%USERPROFILE%*** but can be edited in the `appsettings.json` file. Logging will be expanded in each new release.

###### Version 3.1.0 Updates:
1. Exposed the Labelary API URL in global settings to allow HTTPS or HTTP.
2. Added the option to use POST or GET in global settings.
3. Added an option to enable Linting in global settings. Linting is an option provided by Labelary to provide warnings for the ZPL text sent to the API.
4. Added a viewer for the ZPL warnings. The viewer can be accesses via the context menu on an image or by clicking the button on the label image (which is only visible when warnings exist).
5. Added a preview button on the label image to open the image viewer.
6. Made the connection test viewer output read-only.

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
